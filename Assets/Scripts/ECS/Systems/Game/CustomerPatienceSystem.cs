using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// 손님의 인내심 관리 및 상태 변화 시스템
    /// (가독성을 위해 로직 루프를 분리함)
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CustomerPatienceSystem : SystemBase
    {
        // 대기열 업데이트 정보를 담을 구조체
        private struct QueueUpdateInfo
        {
            public Entity StationEntity;
            public int LeavingIndex;
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            // 대기열 변동 사항을 기록할 리스트
            var queueUpdates = new NativeList<QueueUpdateInfo>(Allocator.Temp);

            // ========================================================================
            // LOOP 1: 살아있는 손님 (인내심 감소 & 상태 변화 로직)
            // ========================================================================
            foreach (var (customer, entity) in
                     SystemAPI.Query<RefRW<CustomerComponent>>()
                     .WithAll<CustomerTag>()
                     .WithEntityAccess())
            {
                // ?? Leaving 상태면 이 루프는 건너뜀 (다음 루프에서 처리)
                if (customer.ValueRO.State == CustomerState.Leaving_Happy ||
                    customer.ValueRO.State == CustomerState.Leaving_Angry)
                    continue;

                CustomerState currentState = customer.ValueRO.State;

                // 대기 중인 상태들 (줄 서기, 주문 중, 늦음)
                if (currentState == CustomerState.WaitingInQueue ||
                    currentState == CustomerState.Ordering ||
                    currentState == CustomerState.WaitingLate)
                {
                    customer.ValueRW.Patience -= deltaTime;

                    // A. 화남 상태로 전환 (30% 미만 & 아직 화 안 냄)
                    if (currentState != CustomerState.WaitingLate &&
                        customer.ValueRO.Patience < (customer.ValueRO.MaxPatience * 0.3f))
                    {
                        customer.ValueRW.State = CustomerState.WaitingLate;
                        // (비주얼 시스템이 Angry 표정 지음)
                    }

                    // B. 실패 퇴장 결심 (0초 이하)
                    if (customer.ValueRO.Patience <= 0f)
                    {
                        // 상태 변경 -> Leaving_Angry (울면서 나감)
                        customer.ValueRW.State = CustomerState.Leaving_Angry;

                        // 실패 카운트 증가 (딱 한 번만 실행됨)
                        if (SystemAPI.TryGetSingletonRW<GameSessionComponent>(out var session))
                        {
                            session.ValueRW.CurrentFailures++;
                            session.ValueRW.ProcessedCount++;

                            Debug.Log($"[실패 누적] {session.ValueRW.CurrentFailures}/{session.ValueRW.MaxFailures}");
                        }

                        Debug.Log($"[손님 {customer.ValueRO.CustomerID}] 너무 늦어! (3초 뒤 퇴장)");
                    }
                }
            }

            // ========================================================================
            // LOOP 2: 떠나는 손님 (퇴장 타이머 & 삭제 로직)
            // ========================================================================
            foreach (var (customer, entity) in
                     SystemAPI.Query<RefRW<CustomerComponent>>()
                     .WithAll<CustomerTag>()
                     .WithEntityAccess())
            {
                // ?? Leaving 상태가 아니면 건너뜀
                if (customer.ValueRO.State != CustomerState.Leaving_Happy &&
                    customer.ValueRO.State != CustomerState.Leaving_Angry)
                    continue;

                // 타이머 초기화 (처음 진입 시)
                if (customer.ValueRO.LeaveTimer == 0)
                {
                    customer.ValueRW.LeaveTimer = 3.0f; // 3초 애니메이션 시간
                }

                // 시간 깎기
                customer.ValueRW.LeaveTimer -= deltaTime;

                // 시간이 다 됨 -> 진짜 삭제
                if (customer.ValueRO.LeaveTimer <= 0)
                {
                    // 대기열 감소 & 뒷사람 땡기기 정보 기록
                    DecreaseQueueCount(customer.ValueRO.TargetStation);
                    queueUpdates.Add(new QueueUpdateInfo
                    {
                        StationEntity = customer.ValueRO.TargetStation,
                        LeavingIndex = customer.ValueRO.QueueIndex
                    });

                    ecb.DestroyEntity(entity);
                    Debug.Log($"[손님 {customer.ValueRO.CustomerID}] 사라짐 & 뒷사람 호출!");
                }
            }

            // ==========================================================
            // 3. 대기열 당기기 (후처리)
            // ==========================================================
            if (!queueUpdates.IsEmpty)
            {
                foreach (var updateInfo in queueUpdates)
                {
                    foreach (var customer in SystemAPI.Query<RefRW<CustomerComponent>>())
                    {
                        // 같은 줄에 있고, 나간 사람보다 뒤에 있는가?
                        if (customer.ValueRO.TargetStation == updateInfo.StationEntity &&
                            customer.ValueRO.QueueIndex > updateInfo.LeavingIndex)
                        {
                            customer.ValueRW.QueueIndex--; // 번호표 당기기

                            // 이동 명령 (대기 중인 사람만)
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
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        // 헬퍼 함수: 대기열 수 줄이기
        private void DecreaseQueueCount(Entity stationEntity)
        {
            if (SystemAPI.Exists(stationEntity) && SystemAPI.HasComponent<ServingStationComponent>(stationEntity))
            {
                var serving = SystemAPI.GetComponent<ServingStationComponent>(stationEntity);
                if (serving.CurrentQueueCount > 0)
                {
                    serving.CurrentQueueCount--;
                    SystemAPI.SetComponent(stationEntity, serving);
                }
            }
        }
    }
}