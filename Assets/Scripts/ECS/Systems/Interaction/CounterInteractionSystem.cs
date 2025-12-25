using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Meow.ECS.Components;
using Meow.ECS.Utils;
using Meow.Audio;

namespace Meow.ECS.Systems
{
    /// <summary>카운터 상호작용: 놓기/집기</summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InteractionSystem))]
    public partial struct CounterInteractionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<GamePauseComponent>(out var pause) && pause.IsPaused) return;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (request, playerState, playerTransform, entity) in
                     SystemAPI.Query<RefRO<InteractionRequestComponent>, RefRW<PlayerStateComponent>, RefRO<LocalTransform>>()
                              .WithAll<CounterRequestTag>()
                              .WithEntityAccess())
            {
                Entity counterEntity = request.ValueRO.TargetStation;

                var counterData = SystemAPI.GetComponent<CounterComponent>(counterEntity);
                var itemBuffer = SystemAPI.GetBuffer<CounterItemSlot>(counterEntity);
                var snapBuffer = SystemAPI.GetBuffer<CounterSnapPoint>(counterEntity);
                var counterTransform = SystemAPI.GetComponent<LocalTransform>(counterEntity);

                if (playerState.ValueRO.IsHoldingItem)
                {
                    if (itemBuffer.Length < counterData.MaxItems)
                    {
                        Entity heldItem = playerState.ValueRO.HeldItemEntity;
                        itemBuffer.Add(new CounterItemSlot { ItemEntity = heldItem });

                        int slotIndex = itemBuffer.Length - 1;
                        float3 snapLocalPos = (slotIndex < snapBuffer.Length)
                            ? snapBuffer[slotIndex].LocalPosition
                            : new float3(0, 1.0f, 0);

                        float3 worldPos = counterTransform.Position + math.rotate(counterTransform.Rotation, snapLocalPos);

                        var stateCopy = playerState.ValueRO;
                        InteractionHelper.DetachItemFromPlayer(
                            ref ecb,
                            entity,
                            ref stateCopy,
                            heldItem,
                            counterEntity,
                            worldPos,
                            itemHasLocalTransform: true,
                            itemHasHoldable: true
                        );
                        playerState.ValueRW = stateCopy;

                        AppendAudioEvent(counterEntity, ref ecb, ref state, SfxId.Pickup);
                    }
                }
                else
                {
                    if (itemBuffer.Length > 0)
                    {
                        int lastIndex = itemBuffer.Length - 1;
                        Entity targetItem = itemBuffer[lastIndex].ItemEntity;
                        itemBuffer.RemoveAt(lastIndex);

                        var stateCopy = playerState.ValueRO;
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
                        playerState.ValueRW = stateCopy;

                        AppendAudioEvent(counterEntity, ref ecb, ref state, SfxId.Pickup);
                    }
                }

                InteractionHelper.EndRequest<CounterRequestTag>(ref ecb, entity);
            }
        }

        private static DynamicBuffer<AudioEvent> EnsureAudioBuffer(Entity target, ref EntityCommandBuffer ecb, ref SystemState state)
        {
            if (state.EntityManager.HasBuffer<AudioEvent>(target))
                return state.EntityManager.GetBuffer<AudioEvent>(target);
            return ecb.AddBuffer<AudioEvent>(target);
        }

        private static void AppendAudioEvent(Entity target, ref EntityCommandBuffer ecb, ref SystemState state, SfxId sfx)
        {
            var buffer = EnsureAudioBuffer(target, ref ecb, ref state);
            buffer.Add(new AudioEvent { Sfx = sfx, Is2D = true });
        }
    }
}
