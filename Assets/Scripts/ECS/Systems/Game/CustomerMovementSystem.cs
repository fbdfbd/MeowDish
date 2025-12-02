using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CustomerMovementSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // =================================================================
            // 1. 줄 찾기 (FindingLine)
            // =================================================================
            foreach (var (customer, entity) in
                     SystemAPI.Query<RefRW<CustomerComponent>>()
                     .WithAll<CustomerTag>()
                     .WithEntityAccess())
            {
                // 막 태어났다면 -> 바로 줄 찾기 상태로 변경
                if (customer.ValueRO.State == CustomerState.Spawned)
                {
                    customer.ValueRW.State = CustomerState.FindingLine;
                }

                // 줄 찾기 상태가 아니면 패스
                if (customer.ValueRO.State != CustomerState.FindingLine) continue;

                // -------------------------------------------------------------
                // 눈치 게임: 가장 줄이 짧은 서빙 카운터 찾기
                // -------------------------------------------------------------
                Entity bestStation = Entity.Null;
                int bestQueueCount = int.MaxValue;

                // 모든 서빙 카운터를 뒤져봄
                foreach (var (serving, stationEntity) in
                         SystemAPI.Query<RefRW<ServingStationComponent>>()
                         .WithEntityAccess())
                {
                    // 자리가 있고(MaxCapacity 미만), 지금 본 곳보다 줄이 짧으면 선택
                    if (serving.ValueRO.CurrentQueueCount < serving.ValueRO.MaxQueueCapacity)
                    {
                        if (serving.ValueRO.CurrentQueueCount < bestQueueCount)
                        {
                            bestQueueCount = serving.ValueRO.CurrentQueueCount;
                            bestStation = stationEntity;
                        }
                    }
                }

                // 갈 곳을 찾았다!
                if (bestStation != Entity.Null)
                {
                    // 서빙 카운터 줄카운트 미리 증가 (자리 찜)
                    var servingData = SystemAPI.GetComponent<ServingStationComponent>(bestStation);

                    // 손님에게 정보 입력
                    customer.ValueRW.TargetStation = bestStation;
                    customer.ValueRW.QueueIndex = servingData.CurrentQueueCount; // 내 번호표 (0, 1, 2...)
                    customer.ValueRW.State = CustomerState.MovingToLine; // "이동 시작!"

                    // 카운터 정보 갱신 (줄 늘어남)
                    servingData.CurrentQueueCount++;
                    SystemAPI.SetComponent(bestStation, servingData);

                    Debug.Log($"[손님 {customer.ValueRO.CustomerID}] {customer.ValueRO.QueueIndex}번째로 줄 서러 갑니다! (타겟: {bestStation.Index})");
                }
                else
                {
                    // 꽉 찼으면... 일단 대기 (나중에 '배회하기' 등을 넣을 수 있음)
                    // Debug.Log("[손님] 자리가 없어요 ㅠㅠ");
                }
            }

            // =================================================================
            // 2. 이동 (MovingToLine) -> 대기 (WaitingInQueue)
            // =================================================================
            foreach (var (customer, transform) in
                     SystemAPI.Query<RefRW<CustomerComponent>, RefRW<LocalTransform>>()
                     .WithAll<CustomerTag>())
            {
                // 이동 상태가 아니면 패스
                if (customer.ValueRO.State != CustomerState.MovingToLine) continue;

                Entity targetStation = customer.ValueRO.TargetStation;

                // 타겟이 사라졌으면(에러 방지)
                if (targetStation == Entity.Null || !SystemAPI.Exists(targetStation)) continue;

                // -------------------------------------------------------------
                // A. 목표 좌표 계산
                // -------------------------------------------------------------
                var stationTransform = SystemAPI.GetComponent<LocalTransform>(targetStation);
                var queuePoint = SystemAPI.GetComponent<ServingQueuePoint>(targetStation);

                // 공식: 시작점 + (간격 * 내순번)
                // (카운터의 회전값도 고려해서 로컬->월드 변환)
                float3 queueOffset = queuePoint.StartLocalPosition + new float3(0, 0, queuePoint.QueueInterval * customer.ValueRO.QueueIndex);
                float3 targetPos = stationTransform.Position + math.rotate(stationTransform.Rotation, queueOffset);

                // -------------------------------------------------------------
                // B. 이동 로직
                // -------------------------------------------------------------
                float3 dir = targetPos - transform.ValueRO.Position;
                dir.y = 0; // 높이 무시 (평면 이동)

                float distSq = math.lengthsq(dir);

                // 도착 판정 (거리가 아주 가까우면)
                if (distSq < 0.05f)
                {
                    // 위치 딱 맞추기
                    transform.ValueRW.Position = new float3(targetPos.x, transform.ValueRO.Position.y, targetPos.z);

                    // 상태 변경: 도착했으니 대기
                    customer.ValueRW.State = CustomerState.WaitingInQueue;

                    // 만약 내가 맨 앞(0번)이라면? -> 바로 주문 상태!
                    if (customer.ValueRO.QueueIndex == 0)
                    {
                        customer.ValueRW.State = CustomerState.Ordering;
                        Debug.Log($"[손님 {customer.ValueRO.CustomerID}] 주문할게요!");
                    }
                }
                else
                {
                    // 걷기
                    float3 moveDir = math.normalize(dir);
                    float moveSpeed = customer.ValueRO.WalkSpeed;

                    transform.ValueRW.Position += moveDir * moveSpeed * deltaTime;

                    // 회전 (나아가는 방향 보기)
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