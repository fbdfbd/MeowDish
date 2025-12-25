using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct StoveCookingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StoveComponent>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<GamePauseComponent>(out var pause) && pause.IsPaused)
                return;

            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (stoveState, stoveData, entity) in
                     SystemAPI.Query<RefRW<StoveCookingState>, RefRO<StoveComponent>>()
                              .WithEntityAccess())
            {
                if (!stoveState.ValueRO.IsCooking)
                {
                    EmitStopFxIfNeeded(entity, stoveState, ref state, ref ecb);
                    continue;
                }

                var itemEntity = stoveState.ValueRO.ItemEntity;

                if (itemEntity == Entity.Null || !SystemAPI.Exists(itemEntity))
                {
                    stoveState.ValueRW.IsCooking = false;
                    stoveState.ValueRW.ItemEntity = Entity.Null;
                    stoveState.ValueRW.CurrentCookProgress = 0f;
                    EmitStopFxIfNeeded(entity, stoveState, ref state, ref ecb);
                    continue;
                }

                if (!SystemAPI.HasComponent<CookableComponent>(itemEntity))
                {
                    stoveState.ValueRW.IsCooking = false;
                    stoveState.ValueRW.ItemEntity = Entity.Null;
                    stoveState.ValueRW.CurrentCookProgress = 0f;
                    EmitStopFxIfNeeded(entity, stoveState, ref state, ref ecb);
                    continue;
                }

                var cookable = SystemAPI.GetComponent<CookableComponent>(itemEntity);
                var item = SystemAPI.GetComponent<ItemComponent>(itemEntity);

                float progress = stoveState.ValueRO.CurrentCookProgress;
                if (SystemAPI.HasComponent<CookingState>(itemEntity))
                {
                    progress = SystemAPI.GetComponent<CookingState>(itemEntity).Elapsed;
                }

                progress += deltaTime * stoveData.ValueRO.CookingSpeedMultiplier;
                stoveState.ValueRW.CurrentCookProgress = progress;

                if (SystemAPI.HasComponent<CookingState>(itemEntity))
                {
                    ecb.SetComponent(itemEntity, new CookingState { Elapsed = progress });
                }
                else
                {
                    ecb.AddComponent(itemEntity, new CookingState { Elapsed = progress });
                }

                if (item.State == ItemState.Raw && progress >= cookable.CookTime)
                {
                    ecb.SetComponent(itemEntity, new ItemComponent
                    {
                        ItemID = item.ItemID,
                        Type = item.Type,
                        IngredientType = item.IngredientType,
                        State = ItemState.Cooked
                    });

                    ecb.RemoveComponent<RawItemTag>(itemEntity);
                    ecb.AddComponent<CookedItemTag>(itemEntity);
                }

                if (cookable.BurnTime > 0 &&
                    item.State == ItemState.Cooked &&
                    progress >= (cookable.CookTime + cookable.BurnTime) &&
                    SystemAPI.HasComponent<BurnableTag>(itemEntity))
                {
                    ecb.SetComponent(itemEntity, new ItemComponent
                    {
                        ItemID = item.ItemID,
                        Type = item.Type,
                        IngredientType = item.IngredientType,
                        State = ItemState.Burnt
                    });

                    ecb.RemoveComponent<CookedItemTag>(itemEntity);
                    ecb.AddComponent<BurnedItemTag>(itemEntity);

                    stoveState.ValueRW.IsCooking = false;
                    EmitBurnedFx(entity, itemEntity, ref state, ref ecb);
                }
            }
        }

        private static void EmitStopFxIfNeeded(Entity stoveEntity, RefRW<StoveCookingState> stoveState, ref SystemState state, ref EntityCommandBuffer ecb)
        {
            var current = stoveState.ValueRO;
            if (current.SfxLoopHandle == 0 && current.SmokeLoopHandle == 0)
                return;

            var buffer = EnsureFxBuffer(stoveEntity, ref state, ref ecb);
            buffer.Add(new StoveFxEvent
            {
                Event = StoveFxEvent.Kind.StopCook,
                WorldPos = float3.zero,
                Rot = quaternion.identity,
                Item = current.ItemEntity
            });
        }

        private static void EmitBurnedFx(Entity stoveEntity, Entity itemEntity, ref SystemState state, ref EntityCommandBuffer ecb)
        {
            var buffer = EnsureFxBuffer(stoveEntity, ref state, ref ecb);
            buffer.Add(new StoveFxEvent
            {
                Event = StoveFxEvent.Kind.Burned,
                WorldPos = float3.zero,
                Rot = quaternion.identity,
                Item = itemEntity
            });
        }

        private static DynamicBuffer<StoveFxEvent> EnsureFxBuffer(Entity stoveEntity, ref SystemState state, ref EntityCommandBuffer ecb)
        {
            if (state.EntityManager.HasBuffer<StoveFxEvent>(stoveEntity))
                return state.EntityManager.GetBuffer<StoveFxEvent>(stoveEntity);
            return ecb.AddBuffer<StoveFxEvent>(stoveEntity);
        }
    }
}
