# NetCode Implementation Templates & Quick-Start Guide

## Quick Reference: What Each System Should Do

### PlayerInputSystem → Split into 2 Systems

❌ **OLD (Delete):**
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial class PlayerInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Can't run on server, can't predict properly
    }
}
```

✅ **NEW:**

```csharp
// Only runs on local client (not server)
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[BurstCompile]
public partial struct LocalInputGatherSystem : ISystem
{
    private float2 _cachedInput;
    private bool _cachedInteract;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<LocalPlayerTag>();  // Only owner
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Gather raw input (only here!)
        float2 moveInput = float2.zero;
        
        // Keyboard (or joystick if available)
        if (Input.GetKey(KeyCode.W)) moveInput.y = 1;
        if (Input.GetKey(KeyCode.S)) moveInput.y = -1;
        if (Input.GetKey(KeyCode.A)) moveInput.x = -1;
        if (Input.GetKey(KeyCode.D)) moveInput.x = 1;
        
        bool interactPressed = Input.GetKeyDown(KeyCode.E);
        
        // Store for next system (no state changes here!)
        _cachedInput = moveInput;
        _cachedInteract = interactPressed;
        
        // Update PlayerInputComponent ONLY for visualization
        // Don't use this for actual movement (prediction)
        SystemAPI.SetSingleton(new RawInputSingleton
        {
            MoveInput = moveInput,
            InteractPressed = interactPressed,
            Tick = GetCurrentNetworkTick()
        });
    }
}

// Helper singleton for inter-system communication
public struct RawInputSingleton : IComponentData
{
    public float2 MoveInput;
    public bool InteractPressed;
    public uint Tick;
}

// Marker for local player
public struct LocalPlayerTag : IComponentData { }
```

---

### PlayerMovementSystem → Add Owner Check

❌ **PROBLEM (Current):**
```csharp
foreach (var (transform, input, stats) in
    SystemAPI.Query<RefRW<LocalTransform>, 
                   RefRO<PlayerInputComponent>,
                   RefRO<PlayerStatsComponent>>())
{
    // Runs on ALL entities, even ghosts!
    transform.ValueRW.Position += ...;
}
```

✅ **SOLUTION:**
```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct PlayerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerInputComponent>();
        state.RequireForUpdate<NetworkOwner>();  // ADD THIS
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var localPlayers = SystemAPI.GetSingleton<LocalPlayerTag>();
        
        foreach (var (transform, input, stats, owner) in
            SystemAPI.Query<RefRW<LocalTransform>, 
                           RefRO<PlayerInputComponent>,
                           RefRO<PlayerStatsComponent>,
                           RefRO<NetworkOwner>>())
        {
            // Only owner should move (prediction on client, authority on server)
            float2 moveInput = input.ValueRO.MoveInput;
            
            if (math.lengthsq(moveInput) < 0.001f)
                continue;
            
            float3 moveDir = new float3(moveInput.x, 0, moveInput.y);
            moveDir = math.normalize(moveDir);
            
            float finalSpeed = stats.ValueRO.GetFinalMoveSpeed();
            
            transform.ValueRW.Position += moveDir * finalSpeed * deltaTime;
            
            quaternion targetRot = quaternion.LookRotation(moveDir, math.up());
            transform.ValueRW.Rotation = math.slerp(
                transform.ValueRO.Rotation,
                targetRot,
                stats.ValueRO.RotationSpeed * deltaTime
            );
        }
    }
}
```

---

### PlayerAnimationSystem → Base on Movement, Not Input

❌ **PROBLEM (Current):**
```csharp
// Animation de-syncs on remote clients
float inputMag = math.length(input.ValueRO.MoveInput);
animation.ValueRW.IsMoving = inputMag > 0.1f;
```

✅ **SOLUTION:**
```csharp
// Add to PlayerAuthoring:
_entityManager.AddComponentData(_playerEntity, 
    new PreviousPositionComponent { Position = transform.position });

// New component:
public struct PreviousPositionComponent : IComponentData
{
    public float3 Position;
}

// Updated system:
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PlayerMovementSystem))]
public partial struct PlayerAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerAnimationComponent>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (animation, transform, prevPos) in
            SystemAPI.Query<RefRW<PlayerAnimationComponent>, 
                           RefRO<LocalTransform>,
                           RefRW<PreviousPositionComponent>>())
        {
            // Calculate actual movement (works for all clients!)
            float3 movementDelta = transform.ValueRO.Position - prevPos.ValueRO.Position;
            float movementMag = math.length(movementDelta);
            
            animation.ValueRW.IsMoving = movementMag > 0.01f;
            
            // Update for next frame
            prevPos.ValueRW.Position = transform.ValueRO.Position;
        }
    }
}
```

---

### ContainerSystem → Server Authority + RPC

❌ **CRITICAL PROBLEM:**
```csharp
private int _nextItemID = 1;  // Different on each client!

ecb.AddComponent(newItem, new ItemComponent
{
    ItemID = _nextItemID++,  // Creates desync!
});
```

✅ **SOLUTION - Three-Part System:**

**Part 1: Client Request (RPC)**
```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ClientTakeItemRequest : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rpcQueue = SystemAPI.GetSingleton<RpcQueue>();
        
        foreach (var (playerState, input, owner) in
            SystemAPI.Query<RefRO<PlayerStateComponent>, 
                           RefRO<PlayerInputComponent>,
                           RefRO<NetworkOwner>>()
            .WithAll<LocalPlayerTag>())
        {
            if (!input.ValueRO.InteractPressed || 
                !playerState.ValueRO.IsNearStation)
                continue;
            
            // Send RPC to server
            var request = new TakeItemRequestRpc
            {
                StationNetID = GetNetID(playerState.ValueRO.CurrentStationEntity),
                ClientRequestTick = GetNetworkTick()
            };
            
            rpcQueue.Schedule(request);
        }
    }
}

[GhostComponent(PrefabType = GhostPrefabType.All)]
public struct TakeItemRequestRpc : IRpcCommand
{
    public uint StationNetID;
    public uint ClientRequestTick;
    
    public void Deserialize(DataStreamReader reader)
    {
        StationNetID = reader.ReadUInt();
        ClientRequestTick = reader.ReadUInt();
    }
    
    public void Serialize(DataStreamWriter writer)
    {
        writer.WriteUInt(StationNetID);
        writer.WriteUInt(ClientRequestTick);
    }
}
```

**Part 2: Server Authority (RPC Handler)**
```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerItemAuthority : ISystem
{
    private int _nextItemID = 1;
    
    public void OnUpdate(ref SystemState state)
    {
        var rpcQueue = SystemAPI.GetSingleton<RpcQueue>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        // Process all client requests
        foreach (var request in rpcQueue.Receive<TakeItemRequestRpc>())
        {
            // Validate
            var station = FindEntityByNetID(request.StationNetID);
            
            if (!IsValidPickup(station))
                continue;  // Reject invalid request
            
            // Create item with SERVER-ASSIGNED ID
            int networkItemID = _nextItemID++;
            var itemEntity = ecb.CreateEntity();
            
            ecb.AddComponent(itemEntity, new NetworkEntity 
            { 
                NetworkID = networkItemID 
            });
            
            ecb.AddComponent(itemEntity, new ItemComponent
            {
                ItemID = networkItemID,  // ✅ Server-assigned
                Type = ItemType.Ingredient,
                State = ItemState.Raw,
                IngredientType = GetContainerIngredient(station)
            });
            
            ecb.AddComponent(itemEntity, 
                LocalTransform.FromPosition(GetStationPosition(station) + new float3(0, 1, 0)));
            
            // Broadcast to all clients
            var @event = new ItemCreatedEvent
            {
                ItemNetID = networkItemID,
                IngredientType = GetContainerIngredient(station),
                Position = GetStationPosition(station)
            };
            
            BroadcastEvent(@event);
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    
    private bool IsValidPickup(Entity station)
    {
        // Validate proximity, permissions, etc
        return true;
    }
}

[GhostComponent(PrefabType = GhostPrefabType.All)]
public struct ItemCreatedEvent : IComponentData
{
    public uint ItemNetID;
    public IngredientType IngredientType;
    public float3 Position;
}
```

**Part 3: Client Receives (Event Handler)**
```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ClientItemSync : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var eventStream = SystemAPI.GetSingleton<EventStream>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        foreach (var @event in eventStream.Receive<ItemCreatedEvent>())
        {
            // Create entity with SERVER'S ID
            var itemEntity = ecb.CreateEntity();
            
            ecb.AddComponent(itemEntity, new NetworkEntity
            {
                NetworkID = @event.ItemNetID
            });
            
            ecb.AddComponent(itemEntity, new ItemComponent
            {
                ItemID = @event.ItemNetID,  // ✅ Matches server!
                Type = ItemType.Ingredient,
                State = ItemState.Raw,
                IngredientType = @event.IngredientType
            });
            
            ecb.AddComponent(itemEntity, 
                LocalTransform.FromPosition(@event.Position));
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

---

### InteractionSystem → RPC + Server Authority

Similar to ContainerSystem:
1. Client sends `InteractionRequestRpc`
2. Server validates and processes
3. Server broadcasts `InteractionResultEvent`
4. All clients apply result

---

### ItemVisualSystem → ECS Rendering (No GameObjects)

❌ **PROBLEM:**
```csharp
private Dictionary<Entity, GameObject> itemVisuals = new();

GameObject visual = CreateItemVisual(item);
itemVisuals[itemEntity] = visual;  // Can't replicate!
visual.transform.position = holderTransform.Position + offset;
```

✅ **SOLUTION - Pure ECS Rendering:**
```csharp
[BurstCompile]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial struct ItemVisualSystem : ISystem
{
    private Material _itemMaterial;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _itemMaterial = new Material(Shader.Find("Standard"));
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Render all items via Graphics.DrawMesh()
        foreach (var (item, transform, holdable) in
            SystemAPI.Query<RefRO<ItemComponent>, 
                           RefRO<LocalTransform>,
                           RefRO<HoldableComponent>>())
        {
            float3 renderPos = transform.ValueRO.Position;
            quaternion renderRot = transform.ValueRO.Rotation;
            
            // If held, render at holder's position
            if (holdable.ValueRO.IsHeld && 
                SystemAPI.Exists(holdable.ValueRO.HolderEntity))
            {
                var holderTransform = SystemAPI.GetComponent<LocalTransform>(
                    holdable.ValueRO.HolderEntity);
                
                renderPos = holderTransform.Position + new float3(0, 1.5f, 0.5f);
                renderRot = holderTransform.Rotation;
            }
            
            var mesh = GetMeshForIngredient(item.ValueRO.IngredientType);
            var color = GetColorForIngredient(item.ValueRO.IngredientType);
            
            _itemMaterial.color = color;
            
            Graphics.DrawMesh(
                mesh,
                Matrix4x4.TRS(renderPos, renderRot, Vector3.one * 0.3f),
                _itemMaterial,
                layer: 0
            );
        }
    }
    
    private Mesh GetMeshForIngredient(IngredientType type)
    {
        // Return simple mesh (Cube, Sphere, etc)
        // Or cache meshes in constructor
        return Resources.Load<Mesh>($"Meshes/Item_{type}");
    }
    
    private float4 GetColorForIngredient(IngredientType type)
    {
        return type switch
        {
            IngredientType.Bread => new float4(0.9f, 0.7f, 0.4f, 1f),
            IngredientType.Meat => new float4(0.8f, 0.2f, 0.2f, 1f),
            IngredientType.Lettuce => new float4(0.2f, 0.8f, 0.2f, 1f),
            _ => new float4(1, 0.3f, 1, 1)
        };
    }
}
```

---

## Required New Components

```csharp
// Network/Multiplayer
namespace Meow.ECS.Components.Network
{
    public struct NetworkOwner : IComponentData
    {
        public uint ClientNetID;
    }
    
    public struct NetworkEntity : IComponentData
    {
        public uint NetworkID;  // Unique across all clients
    }
    
    public struct LocalPlayerTag : IComponentData { }
    
    public struct RawInputSingleton : IComponentData
    {
        public float2 MoveInput;
        public bool InteractPressed;
        public uint Tick;
    }
    
    // For animation fix
    public struct PreviousPositionComponent : IComponentData
    {
        public float3 Position;
    }
}
```

---

## Updated PlayerAuthoring.cs

```csharp
public class PlayerAuthoring : MonoBehaviour
{
    // ... existing fields ...
    
    private void Start()
    {
        // ... existing setup ...
        
        // ADD THESE:
        
        // Network ownership
        _entityManager.AddComponentData(_playerEntity, new NetworkOwner
        {
            ClientNetID = GetLocalClientNetID()  // Your network system
        });
        
        // Network entity ID
        _entityManager.AddComponentData(_playerEntity, new NetworkEntity
        {
            NetworkID = AssignNetworkID()  // Unique per client
        });
        
        // For animation system
        _entityManager.AddComponentData(_playerEntity, 
            new PreviousPositionComponent
            {
                Position = transform.position
            });
        
        // Mark as local player if this is the owner
        if (IsLocalPlayerOwner())
        {
            _entityManager.AddComponentData(_playerEntity, new LocalPlayerTag());
        }
    }
}
```

---

## Migration Checklist

### Phase 1: Minimal NetCode Setup
- [ ] Create NetworkOwner & NetworkEntity components
- [ ] Add NetworkEntity to PlayerAuthoring
- [ ] Update PlayerMovementSystem to check NetworkOwner
- [ ] Test: Single player still works

### Phase 2: Input Refactor
- [ ] Create LocalInputGatherSystem (only gathers input)
- [ ] Remove state changes from PlayerInputSystem
- [ ] Update PlayerMovementSystem to use local input
- [ ] Test: Movement still works

### Phase 3: Item System
- [ ] Create ServerItemAuthority system (server-only)
- [ ] Create ClientItemSync system (all clients)
- [ ] Create TakeItemRequestRpc & ItemCreatedEvent
- [ ] Remove direct item creation from ContainerSystem
- [ ] Test: Items sync across clients

### Phase 4: Rendering
- [ ] Migrate ItemVisualSystem to Graphics.DrawMesh()
- [ ] Remove GameObject creation
- [ ] Test: Items render without GameObjects

### Phase 5: Interactions
- [ ] Create ServerInteractionAuthority (server-only)
- [ ] Create ClientInteractionFeedback (all clients)
- [ ] Create InteractionRequestRpc & InteractionResultEvent
- [ ] Test: Interactions sync across network

---

## Testing Each Phase

```csharp
public class NetCodeTests
{
    private World _serverWorld;
    private World _clientWorld;
    
    [Test]
    public void Test_PlayerMovementSystem_ChecksNetworkOwner()
    {
        // Create server world
        _serverWorld = new World("TestServer");
        var serverManager = _serverWorld.EntityManager;
        
        // Create player with NetworkOwner
        var player = serverManager.CreateEntity();
        serverManager.AddComponentData(player, new LocalTransform());
        serverManager.AddComponentData(player, new PlayerInputComponent 
        { 
            MoveInput = new float2(1, 0) 
        });
        serverManager.AddComponentData(player, new PlayerStatsComponent 
        { 
            BaseMoveSpeed = 5f,
            RotationSpeed = 10f,
            MoveSpeedBonus = 0,
            MoveSpeedMultiplier = 1f,
            ActionSpeedMultiplier = 1f,
            AllSpeedMultiplier = 1f
        });
        serverManager.AddComponentData(player, new NetworkOwner 
        { 
            ClientNetID = 1 
        });
        
        // Run system
        var system = _serverWorld.GetExistingSystemManaged<PlayerMovementSystem>();
        system.Update(_serverWorld.Unmanaged);
        
        // Verify movement happened
        var finalPos = serverManager.GetComponentData<LocalTransform>(player).Position;
        Assert.That(finalPos.x, Is.GreaterThan(0), "Player should have moved");
    }
}
```

