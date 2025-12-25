using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CustomerPatienceSystem : ISystem
    {
        private struct QueueUpdateInfo
        {
            public Entity StationEntity;
            public int LeavingIndex;
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<GamePauseComponent>(out var pause) && pause.IsPaused) return;

            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var queueUpdates = new NativeList<QueueUpdateInfo>(Allocator.Temp);

            foreach (var (customer, entity) in
                     SystemAPI.Query<RefRW<CustomerComponent>>()
                         .WithAll<CustomerTag>()
                         .WithEntityAccess())
            {
                if (customer.ValueRO.State == CustomerState.Leaving_Happy ||
                    customer.ValueRO.State == CustomerState.Leaving_Angry)
                    continue;

                var stateRO = customer.ValueRO;
                if (stateRO.State == CustomerState.WaitingInQueue ||
                    stateRO.State == CustomerState.Ordering ||
                    stateRO.State == CustomerState.WaitingLate)
                {
                    customer.ValueRW.Patience -= deltaTime;

                    if (stateRO.State != CustomerState.WaitingLate &&
                        customer.ValueRW.Patience < (stateRO.MaxPatience * 0.3f))
                    {
                        customer.ValueRW.State = CustomerState.WaitingLate;
                    }

                    if (customer.ValueRW.Patience <= 0f)
                    {
                        customer.ValueRW.State = CustomerState.Leaving_Angry;
                        if (SystemAPI.TryGetSingletonRW<GameSessionComponent>(out var session))
                        {
                            session.ValueRW.CurrentFailures++;
                            session.ValueRW.ProcessedCount++;
                        }
                    }
                }
            }

            foreach (var (customer, entity) in
                     SystemAPI.Query<RefRW<CustomerComponent>>()
                         .WithAll<CustomerTag>()
                         .WithEntityAccess())
            {
                if (customer.ValueRO.State != CustomerState.Leaving_Happy &&
                    customer.ValueRO.State != CustomerState.Leaving_Angry)
                    continue;

                if (customer.ValueRO.LeaveTimer == 0)
                    customer.ValueRW.LeaveTimer = 3.0f;

                customer.ValueRW.LeaveTimer -= deltaTime;

                if (customer.ValueRW.LeaveTimer <= 0)
                {
                    DecreaseQueueCount(customer.ValueRO.TargetStation, ref state);
                    queueUpdates.Add(new QueueUpdateInfo
                    {
                        StationEntity = customer.ValueRO.TargetStation,
                        LeavingIndex = customer.ValueRO.QueueIndex
                    });

                    ecb.DestroyEntity(entity);
                }
            }

            if (!queueUpdates.IsEmpty)
            {
                foreach (var updateInfo in queueUpdates)
                {
                    foreach (var customer in SystemAPI.Query<RefRW<CustomerComponent>>())
                    {
                        if (customer.ValueRO.TargetStation == updateInfo.StationEntity &&
                            customer.ValueRO.QueueIndex > updateInfo.LeavingIndex)
                        {
                            customer.ValueRW.QueueIndex--;
                            if (customer.ValueRO.State == CustomerState.WaitingInQueue ||
                                customer.ValueRO.State == CustomerState.WaitingLate)
                            {
                                customer.ValueRW.State = CustomerState.MovingToLine;
                            }
                        }
                    }
                }
            }

            queueUpdates.Dispose();
        }

        private static void DecreaseQueueCount(Entity stationEntity, ref SystemState state)
        {
            if (state.EntityManager.Exists(stationEntity) &&
                state.EntityManager.HasComponent<ServingStationComponent>(stationEntity))
            {
                var serving = state.EntityManager.GetComponentData<ServingStationComponent>(stationEntity);
                if (serving.CurrentQueueCount > 0)
                {
                    serving.CurrentQueueCount--;
                    state.EntityManager.SetComponentData(stationEntity, serving);
                }
            }
        }
    }
}
