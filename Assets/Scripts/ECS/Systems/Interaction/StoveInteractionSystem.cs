using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Meow.ECS.Components;
using Meow.ECS.Utils;

namespace Meow.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InteractionSystem))]
    public partial struct StoveInteractionSystem : ISystem
    {
        [BurstCompile] public void OnCreate(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<GamePauseComponent>(out var pause) && pause.IsPaused) return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (request, playerStateRW, playerTransform, entity) in
                     SystemAPI.Query<RefRO<InteractionRequestComponent>, RefRW<PlayerStateComponent>, RefRO<LocalTransform>>()
                              .WithAll<StoveRequestTag>()
                              .WithEntityAccess())
            {
                Entity stoveEntity = request.ValueRO.TargetStation;

                var stoveState = SystemAPI.GetComponentRW<StoveCookingState>(stoveEntity);
                var stoveSnap = SystemAPI.GetComponent<StoveSnapPoint>(stoveEntity);
                var stoveTransform = SystemAPI.GetComponent<LocalTransform>(stoveEntity);

                // CASE 1: 플레이어 -> 스토브 (올리기)
                if (playerStateRW.ValueRO.IsHoldingItem)
                {
                    if (stoveState.ValueRO.ItemEntity == Entity.Null)
                    {
                        Entity heldItem = playerStateRW.ValueRO.HeldItemEntity;

                        if (SystemAPI.HasComponent<CookableComponent>(heldItem))
                        {
                            float startProgress = 0f;
                            if (SystemAPI.HasComponent<CookingState>(heldItem))
                                startProgress = SystemAPI.GetComponent<CookingState>(heldItem).Elapsed;
                            else
                                ecb.AddComponent(heldItem, new CookingState { Elapsed = 0f });

                            stoveState.ValueRW.ItemEntity = heldItem;
                            stoveState.ValueRW.CurrentCookProgress = startProgress;
                            stoveState.ValueRW.IsCooking = true;

                            float3 worldPos = stoveTransform.Position + math.rotate(stoveTransform.Rotation, stoveSnap.LocalPosition);

                            var stateCopy = playerStateRW.ValueRO;
                            InteractionHelper.DetachItemFromPlayer(
                                ref ecb,
                                entity,
                                ref stateCopy,
                                heldItem,
                                stoveEntity,
                                worldPos,
                                itemHasLocalTransform: true,
                                itemHasHoldable: true
                            );
                            playerStateRW.ValueRW = stateCopy;

                            var fxBuffer = EnsureFxBuffer(stoveEntity, ref ecb, ref state);
                            fxBuffer.Add(new StoveFxEvent
                            {
                                Event = StoveFxEvent.Kind.StartCook,
                                WorldPos = worldPos,
                                Rot = stoveTransform.Rotation,
                                Item = heldItem
                            });
                        }
                    }
                }
                // CASE 2: 스토브 -> 플레이어 (꺼내기)
                else
                {
                    if (stoveState.ValueRO.ItemEntity != Entity.Null)
                    {
                        Entity targetItem = stoveState.ValueRO.ItemEntity;
                        float carriedProgress = stoveState.ValueRO.CurrentCookProgress;

                        // 진행도 아이템에 기록
                        if (SystemAPI.HasComponent<CookingState>(targetItem))
                            ecb.SetComponent(targetItem, new CookingState { Elapsed = carriedProgress });
                        else
                            ecb.AddComponent(targetItem, new CookingState { Elapsed = carriedProgress });

                        stoveState.ValueRW.ItemEntity = Entity.Null;
                        stoveState.ValueRW.IsCooking = false;
                        stoveState.ValueRW.CurrentCookProgress = 0f;

                        var stateCopy = playerStateRW.ValueRO;
                        InteractionHelper.AttachItemToPlayer(
                            ref ecb,
                            entity,
                            ref stateCopy,
                            playerTransform.ValueRO.Position,
                            playerTransform.ValueRO.Rotation,
                            targetItem,
                            itemHasLocalTransform: true,
                            itemHasHoldable: true
                        );
                        playerStateRW.ValueRW = stateCopy;

                        float3 worldPos = stoveTransform.Position + math.rotate(stoveTransform.Rotation, stoveSnap.LocalPosition);
                        var fxBuffer = EnsureFxBuffer(stoveEntity, ref ecb, ref state);
                        fxBuffer.Add(new StoveFxEvent
                        {
                            Event = StoveFxEvent.Kind.StopCook,
                            WorldPos = worldPos,
                            Rot = stoveTransform.Rotation,
                            Item = targetItem
                        });
                    }
                }

                InteractionHelper.EndRequest<StoveRequestTag>(ref ecb, entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private static DynamicBuffer<StoveFxEvent> EnsureFxBuffer(Entity stoveEntity, ref EntityCommandBuffer ecb, ref SystemState state)
        {
            if (state.EntityManager.HasBuffer<StoveFxEvent>(stoveEntity))
                return state.EntityManager.GetBuffer<StoveFxEvent>(stoveEntity);
            return ecb.AddBuffer<StoveFxEvent>(stoveEntity);
        }
    }
}
