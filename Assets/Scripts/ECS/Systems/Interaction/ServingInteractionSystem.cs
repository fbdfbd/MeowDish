using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Meow.ECS.Components;
using Meow.ECS.Utils;
using Meow.Audio;
using Meow.Managers;

namespace Meow.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InteractionSystem))]
    public partial struct ServingInteractionSystem : ISystem
    {
        [BurstCompile] public void OnCreate(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<GamePauseComponent>(out var pause) && pause.IsPaused) return;

            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (request, playerState, playerTransform, entity) in
                     SystemAPI.Query<RefRO<InteractionRequestComponent>, RefRW<PlayerStateComponent>, RefRO<LocalTransform>>()
                              .WithAll<ServingRequestTag>()
                              .WithEntityAccess())
            {
                if (request.ValueRO.TargetStation == Entity.Null)
                {
                    InteractionHelper.EndRequest<ServingRequestTag>(ref ecb, entity);
                    continue;
                }

                Entity stationEntity = request.ValueRO.TargetStation;

                if (playerState.ValueRO.IsHoldingItem)
                {
                    Entity heldItemEntity = playerState.ValueRO.HeldItemEntity;
                    var itemData = em.GetComponentData<ItemComponent>(heldItemEntity);

                    Entity targetCustomer = Entity.Null;
                    IngredientType orderedDish = IngredientType.None;

                    foreach (var (customer, customerEntity) in
                             SystemAPI.Query<RefRW<CustomerComponent>>()
                             .WithAll<CustomerTag>()
                             .WithEntityAccess())
                    {
                        if (customer.ValueRO.TargetStation == stationEntity &&
                            customer.ValueRO.QueueIndex == 0 &&
                            (customer.ValueRO.State == CustomerState.Ordering ||
                             customer.ValueRO.State == CustomerState.WaitingInQueue ||
                             customer.ValueRO.State == CustomerState.WaitingLate))
                        {
                            targetCustomer = customerEntity;
                            orderedDish = customer.ValueRO.OrderDish;
                            break;
                        }
                    }

                    if (targetCustomer != Entity.Null && itemData.IngredientType == orderedDish)
                    {
                        var stateCopy = playerState.ValueRW;
                        InteractionHelper.DestroyHeldItem(ref ecb, entity, ref stateCopy);
                        playerState.ValueRW = stateCopy;

                        var customerData = em.GetComponentData<CustomerComponent>(targetCustomer);
                        customerData.State = CustomerState.Leaving_Happy;
                        customerData.Patience = 0;
                        ecb.SetComponent(targetCustomer, customerData);

                        if (SystemAPI.TryGetSingletonRW<GameSessionComponent>(out var session))
                        {
                            float scoreMultiplier = session.ValueRO.ScoreMultiplier > 0f ? session.ValueRO.ScoreMultiplier : 1f;
                            session.ValueRW.CurrentScore += (int)math.round(100f * scoreMultiplier);
                            session.ValueRW.ServedCustomers++;
                            session.ValueRW.ProcessedCount++;
                        }

                        // 한 번만 버퍼 확보 후 두 이벤트 추가
                        var audioBuffer = EnsureAudioBuffer(stationEntity, ref ecb, ref state);
                        audioBuffer.Add(new AudioEvent { Sfx = SfxId.Meow, Is2D = true, AllowDuplicate = true });
                        audioBuffer.Add(new AudioEvent { Sfx = SfxId.Coins, Is2D = true, AllowDuplicate = true });

                        float3 fxPos = playerTransform.ValueRO.Position + new float3(0f, 1.2f, 0.2f);
                        AppendServingFxEvent(stationEntity, ref ecb, ref state, ParticleType.ServingSuccess, fxPos);
                    }
                }

                InteractionHelper.EndRequest<ServingRequestTag>(ref ecb, entity);
            }

            ecb.Playback(em);
            ecb.Dispose();
        }

        private static DynamicBuffer<AudioEvent> EnsureAudioBuffer(Entity target, ref EntityCommandBuffer ecb, ref SystemState state)
        {
            if (state.EntityManager.HasBuffer<AudioEvent>(target))
                return state.EntityManager.GetBuffer<AudioEvent>(target);
            return ecb.AddBuffer<AudioEvent>(target);
        }

        private static DynamicBuffer<ServingFxEvent> EnsureServingFxBuffer(Entity target, ref EntityCommandBuffer ecb, ref SystemState state)
        {
            if (state.EntityManager.HasBuffer<ServingFxEvent>(target))
                return state.EntityManager.GetBuffer<ServingFxEvent>(target);
            return ecb.AddBuffer<ServingFxEvent>(target);
        }

        private static void AppendServingFxEvent(Entity target, ref EntityCommandBuffer ecb, ref SystemState state, ParticleType particle, float3 pos)
        {
            var buffer = EnsureServingFxBuffer(target, ref ecb, ref state);
            buffer.Add(new ServingFxEvent { Particle = particle, WorldPos = pos });
        }
    }
}
