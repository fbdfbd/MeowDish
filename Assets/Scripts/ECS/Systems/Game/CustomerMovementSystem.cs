using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CustomerMovementSystem : ISystem
    {
        [BurstCompile] public void OnCreate(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<GamePauseComponent>(out var pause) && pause.IsPaused) return;

            float deltaTime = SystemAPI.Time.DeltaTime;

            // 1) 줄 찾기
            foreach (var (customer, entity) in
                     SystemAPI.Query<RefRW<CustomerComponent>>()
                         .WithAll<CustomerTag>()
                         .WithEntityAccess())
            {
                if (customer.ValueRO.State == CustomerState.Spawned)
                    customer.ValueRW.State = CustomerState.FindingLine;
                if (customer.ValueRO.State != CustomerState.FindingLine) continue;

                Entity bestStation = Entity.Null;
                int bestQueueCount = int.MaxValue;

                foreach (var (serving, stationEntity) in
                         SystemAPI.Query<RefRW<ServingStationComponent>>()
                             .WithEntityAccess())
                {
                    if (serving.ValueRO.CurrentQueueCount < serving.ValueRO.MaxQueueCapacity &&
                        serving.ValueRO.CurrentQueueCount < bestQueueCount)
                    {
                        bestQueueCount = serving.ValueRO.CurrentQueueCount;
                        bestStation = stationEntity;
                    }
                }

                if (bestStation != Entity.Null)
                {
                    var servingData = SystemAPI.GetComponent<ServingStationComponent>(bestStation);

                    customer.ValueRW.TargetStation = bestStation;
                    customer.ValueRW.QueueIndex = servingData.CurrentQueueCount;
                    customer.ValueRW.State = CustomerState.MovingToLine;

                    servingData.CurrentQueueCount++;
                    SystemAPI.SetComponent(bestStation, servingData);
                }
            }

            // 2) 이동 > 대기/주문
            foreach (var (customer, transform) in
                     SystemAPI.Query<RefRW<CustomerComponent>, RefRW<LocalTransform>>()
                         .WithAll<CustomerTag>())
            {
                if (customer.ValueRO.State != CustomerState.MovingToLine) continue;

                Entity targetStation = customer.ValueRO.TargetStation;
                if (targetStation == Entity.Null || !SystemAPI.Exists(targetStation)) continue;

                var stationTransform = SystemAPI.GetComponent<LocalTransform>(targetStation);
                var queuePoint = SystemAPI.GetComponent<ServingQueuePoint>(targetStation);

                float3 queueOffset = queuePoint.StartLocalPosition + new float3(0, 0, queuePoint.QueueInterval * customer.ValueRO.QueueIndex);
                float3 targetPos = stationTransform.Position + math.rotate(stationTransform.Rotation, queueOffset);

                float3 dir = targetPos - transform.ValueRO.Position;
                dir.y = 0;
                float distSq = math.lengthsq(dir);

                if (distSq < 0.05f)
                {
                    transform.ValueRW.Position = new float3(targetPos.x, transform.ValueRO.Position.y, targetPos.z);
                    if (customer.ValueRO.QueueIndex == 0)
                        customer.ValueRW.State = CustomerState.Ordering;
                    else
                        customer.ValueRW.State = CustomerState.WaitingInQueue;
                }
                else
                {
                    float3 moveDir = math.normalize(dir);
                    float moveSpeed = customer.ValueRO.WalkSpeed;
                    transform.ValueRW.Position += moveDir * moveSpeed * deltaTime;

                    if (math.lengthsq(moveDir) > 0.001f)
                    {
                        quaternion targetRot = quaternion.LookRotation(moveDir, math.up());
                        transform.ValueRW.Rotation = math.slerp(transform.ValueRO.Rotation, targetRot, deltaTime * 10f);
                    }
                }
            }
        }
    }
}
