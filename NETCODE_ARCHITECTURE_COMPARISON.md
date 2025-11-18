# Current vs NetCode Architecture Comparison

## Current Architecture (Single-Player)

```
┌─────────────────────────────────────────────────────────────┐
│                    Unity Scene                              │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────────┐         ┌──────────────────────┐      │
│  │  Game Objects   │         │    ECS Systems       │      │
│  │  ─────────────  │         │  ──────────────────  │      │
│  │  • Player       │◄────────┤  PlayerInputSystem   │      │
│  │  • UI/Joystick  │         │  PlayerMovementSys   │      │
│  │  • Items (vis)  │         │  PlayerAnimationSys  │      │
│  │                 │         │  InteractionSystem   │      │
│  └─────────────────┘         │  ContainerSystem     │      │
│         ▲                     │  ItemVisualSystem    │      │
│         │                     └──────────────────────┘      │
│         │                                                   │
│    GameObject.CreatePrimitive()                             │
│    GameObject.Find()                                        │
│    Input.GetKey()                                           │
│    Direct State Changes                                     │
│                                                              │
└─────────────────────────────────────────────────────────────┘
        ▲
        │
        └── Single Client (Local Only)
```

**Issues:**
- No network communication
- Input → State changes immediately
- Item IDs local-only, not synced
- GameObjects can't replicate
- No client/server separation
- No prediction or rollback

---

## Required NetCode Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Network Server                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│  [ServerSimulation World]                                                    │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │ Authority Systems (Run every tick)                                   │   │
│  │ ─────────────────────────────────────────────                        │   │
│  │ • ServerInputProcessor (receives RPC inputs)                         │   │
│  │ • ServerPlayerMovement (authoritative position)                      │   │
│  │ • ServerInteractionAuthority (validates interactions)                │   │
│  │ • ServerItemAuthority (generates network IDs)                        │   │
│  │ • ServerContainerAuthority (validates pickup/drop)                   │   │
│  │                                                                      │   │
│  │  ↓ Broadcast to all clients via:                                    │   │
│  │  • NetworkTransform (player positions)                              │   │
│  │  • NetworkedPlayerState (held items, stations)                      │   │
│  │  • ItemCreatedEvent (item IDs, states)                              │   │
│  │  • InteractionResultEvent (feedback)                                │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
         ▲                                          ▲
         │ RPC Calls                               │ Replication
         │ (Interaction, Item Pickup)              │
         │                                         │
┌────────┴────────────────────────┬────────────────┴─────────────────────────┐
│                                 │                                          │
│  ┌──────────────────────────────┴────────────────┐  ┌──────────────────────┤
│  │       Client A (Local Player)                 │  │  Client B (Remote)   │
│  ├───────────────────────────────────────────────┤  ├──────────────────────┤
│  │ [PredictedSimulation] + [SimulationWorld]    │  │ [SimulationWorld]    │
│  │                                               │  │                      │
│  │ Input Phase:                                  │  │ Replication:         │
│  │ ├─ LocalInputGatherSystem (only here)        │  │ ├─ Parse network     │
│  │ │  Input.GetKey() → InputCommand             │  │ │  data              │
│  │ │  ↓                                          │  │ │  ↓                 │
│  │ ├─ ClientInputBuffer (stores locally)        │  │ ├─ NetworkSync       │
│  │ │  ↓                                          │  │ │  Systems           │
│  │ ├─ Send to Server (RPC)                      │  │ │  ↓                 │
│  │                                               │  │                      │
│  │ Simulation Phase:                             │  │ Rendering:          │
│  │ ├─ PlayerMovementSystem (predicted)          │  │ ├─ ItemVisualSys    │
│  │ │  + Owner Check: only if NetworkOwner       │  │ │  (interpolation)  │
│  │ │  ↓                                          │  │ │  ↓                 │
│  │ ├─ InteractionSystem (owner-only)            │  │ ├─ Ghost Animation   │
│  │ │  ↓                                          │  │ │  (smooth replay)  │
│  │ ├─ Network Replication                       │  │                      │
│  │ │  (receives server updates)                 │  │                      │
│  │ │  ↓                                          │  │                      │
│  │ └─ OnCollisionDetected? Rollback + rerun     │  │                      │
│  │                                               │  │                      │
│  │ Rendering:                                    │  │                      │
│  │ └─ ItemVisualSystem                          │  │                      │
│  │    (local + ghosts with interpolation)       │  │                      │
│  └───────────────────────────────────────────────┘  └──────────────────────┘
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## Data Flow Comparison

### Current (Single-Player)

```
Player Presses 'E'
  ↓
Input.GetKey() captures → PlayerInputComponent.InteractPressed = true
  ↓
InteractionSystem reads PlayerInputComponent
  ↓
PlayerStateComponent.CurrentStationEntity updated immediately
  ↓
ContainerSystem creates item with _nextItemID++
  ↓
ItemVisualSystem spawns GameObject
  ↓
✓ Visual result appears on this client only
```

### NetCode (Multiplayer)

```
CLIENT A: Player Presses 'E'
  ↓
LocalInputGatherSystem: Input.GetKey() → InputCommand.InteractPressed = true
  ↓
ClientInteractionRequestSystem: Only if NetworkOwner == ClientID
  ↓
Send InteractionRequestRpc to SERVER

SERVER: Receives InteractionRequestRpc
  ↓
Validate: Is player near station? Can they interact?
  ↓
ServerInteractionAuthority: Create item, assign ID from server counter
  ↓
NetworkEntity component: ID = 42 (globally unique)
  ↓
Broadcast InteractionResultEvent to all clients

CLIENT A & CLIENT B: Receive InteractionResultEvent
  ↓
ClientInteractionResultSystem: Create item with ID = 42
  ↓
ItemVisualSystem: Render item (local position + networked transform)
  ↓
✓ Both clients show identical item with same ID
```

---

## Component Flow Diagram

### Current Components (Incomplete for Networking)

```
Player Entity:
  ├─ LocalTransform (position/rotation)
  ├─ PlayerInputComponent ─────────┐
  │   ├─ MoveInput (local input)   │ Local-only,
  │   └─ InteractPressed           │ not networked
  │                                 │
  ├─ PlayerStateComponent          │
  │   ├─ PlayerId                  │
  │   ├─ HeldItemEntity ───────────────┐ Entity refs won't
  │   ├─ IsHoldingItem             │  │ sync across network
  │   ├─ CurrentStationEntity ─────────┘
  │   └─ IsNearStation
  │
  ├─ PlayerStatsComponent ─────────────┐
  │   ├─ BaseMoveSpeed              │ ✅ Deterministic,
  │   ├─ RotationSpeed              │   can be networked
  │   └─ Multipliers                │
  │                                 │
  └─ PlayerAnimationComponent
      └─ IsMoving (from local input) ─ ❌ Won't work for ghosts
```

### Required NetCode Components

```
Player Entity:
  ├─ LocalTransform (position/rotation)
  │
  ├─ NetworkTransform ──────────────┐
  │   ├─ NetworkId                  │ Handles replication
  │   ├─ OwnerNetId                 │
  │   └─ Position/Rotation values   │
  │                                 │
  ├─ NetworkOwner ───────────────┐  │
  │   └─ ClientId                 │  │ Ownership check
  │                                 │
  ├─ PlayerInputComponent         │  │
  │   (local-only, not broadcast)  │  │
  │                                 │
  ├─ InputCommand (Predicted) ────────┐
  │   ├─ Tick number                  │ Tick-based for
  │   ├─ MoveInput                    │ prediction/rollback
  │   └─ InteractPressed              │
  │                                 │
  ├─ NetworkedPlayerState ────────────┐
  │   ├─ SelectedStationNetId      │  │ Synced state
  │   ├─ HeldItemNetId             │  │
  │   └─ IsNearStation             │  │
  │                                 │
  ├─ PlayerStatsComponent         │
  │   ├─ BaseMoveSpeed             │ (unchanged)
  │   └─ ...                       │
  │                                 │
  ├─ PlayerAnimationComponent     │
  │   ├─ IsMoving                  │ ❌ Change to movement-based
  │   └─ MovementVelocity ────────────┘
  │
  └─ GhostOwnerComponent (remote only)
      └─ Marks this as networked entity
```

### Item Entity (Old vs New)

**Current (Single-Player):**
```
Item Entity:
  ├─ LocalTransform
  ├─ ItemComponent
  │   ├─ ItemID = 5 (local counter)  ❌ Different on each client!
  │   ├─ Type
  │   ├─ State
  │   └─ IngredientType
  │
  ├─ HoldableComponent
  │   └─ HolderEntity (entity ref)   ❌ Won't sync across network
  │
  └─ ItemVisualTag
      └─ Triggers GameObject creation ❌ Client-side only
```

**Required (NetCode):**
```
Item Entity:
  ├─ LocalTransform (replicated)
  │
  ├─ NetworkEntity ───────────────┐
  │   └─ NetworkId = 42           │ ✅ Server-assigned, unique
  │                               │    across all clients
  ├─ ItemComponent (replicated)
  │   ├─ ItemID = 42 (same as NetId)
  │   ├─ Type
  │   ├─ State (synced)
  │   └─ IngredientType
  │
  ├─ NetworkTransform
  │   └─ Handles position sync
  │
  ├─ HoldableComponent
  │   ├─ HolderNetId (not entity ref!)
  │   └─ IsHeld
  │
  └─ ItemOwnershipTag
      └─ Owner info (for prediction)
```

---

## System Execution Order Comparison

### Current Order

```
SimulationSystemGroup
├─ PlayerInputSystem (gather input)
│  └─ Updates PlayerInputComponent
│
├─ PlayerMovementSystem (uses input)
│  └─ Updates LocalTransform
│
├─ PlayerAnimationSystem (uses input)
│  └─ Updates PlayerAnimationComponent
│
├─ InteractionSystem
│  └─ Reads PlayerInputComponent, updates PlayerStateComponent
│
└─ ContainerSystem
   └─ Creates items, uses EntityCommandBuffer

LateSimulationSystemGroup
└─ ItemVisualSystem (creates GameObjects)
```

**Problem:** All systems run on all clients independently

---

### Required NetCode Order

```
ServerSimulation World:
┌─ Network Simulation Group
│
├─ FixedTickServerPhysicsSystem
├─ ServerInputProcessor (receives RPCs)
│
├─ ServerMovementSystem
│  └─ Updates authoritative positions
│
├─ ServerInteractionAuthority
│  └─ Validates and executes interactions
│
├─ ServerItemAuthority
│  └─ Generates network IDs
│
└─ NetworkUpdateSystems (broadcasts state)
   ├─ Serialize NetworkTransform
   ├─ Serialize PlayerStateComponent
   └─ Send updates to clients

---

PredictedSimulation World (Client A only):
┌─ Prediction System Group
│
├─ LocalInputGatherSystem
│  └─ Input.GetKey() only (not synced)
│
├─ PredictedPlayerMovementSystem
│  └─ Predicts position until server update
│
├─ PredictedInteractionSystem
│  └─ Optimistic interaction feedback
│
└─ OnCollisionDetected?
   └─ Rollback: replay ticks with correct input

---

SimulationWorld (All Clients):
┌─ Network Replication Group
│
├─ NetworkSpawnSystem (creates entities from server)
├─ NetworkDeserializeSystem (parses network data)
│  ├─ NetworkTransform.Position
│  ├─ ItemComponent updates
│  └─ PlayerStateComponent updates
│
├─ ClientInteractionResultSystem
│  └─ Applies server-validated results
│
└─ ClientAnimationSystem
   └─ Updates animation from actual movement (not input)

---

LateSimulationSystemGroup:
├─ ItemVisualSystem (all clients)
│  ├─ Local items: predictive
│  └─ Ghost items: interpolated
│
└─ NetworkTransformVisualizerSystem
   └─ Renders ghost players
```

---

## Critical Differences Summary

| Aspect | Current | NetCode |
|--------|---------|---------|
| **Input Handling** | Direct local input | Local gather + server authority |
| **Item ID Gen** | Static counter per client | Server-assigned unique IDs |
| **State Changes** | Immediate on any client | RPC request → server validates → broadcast |
| **GameObjects** | Created per client | Spawned networked, synced from ECS |
| **Entity Refs** | Entity IDs (local only) | NetworkEntity IDs (global) |
| **Prediction** | N/A | Client predicts, server validates |
| **Rollback** | N/A | Rewind & replay on misprediction |
| **Animation** | Input-based | Movement-based |
| **Burst Compatible** | Some systems | All systems (requirement) |
| **Tick-Based** | Frame-based | Server tick rate + interpolation |

