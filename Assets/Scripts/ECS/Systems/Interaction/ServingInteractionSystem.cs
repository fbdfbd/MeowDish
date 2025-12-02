using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InteractionSystem))]
    public partial class ServingInteractionSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            // 서빙 요청이 있는 플레이어 찾기
            foreach (var (request, playerState, entity) in
                     SystemAPI.Query<RefRO<InteractionRequestComponent>, RefRW<PlayerStateComponent>>()
                         .WithAll<ServingRequestTag>()
                         .WithEntityAccess())
            {
                // [방어 코드] 이미 처리된 요청이면 패스
                if (request.ValueRO.TargetStation == Entity.Null)
                {
                    ecb.RemoveComponent<InteractionRequestComponent>(entity);
                    ecb.RemoveComponent<ServingRequestTag>(entity);
                    continue;
                }

                Entity stationEntity = request.ValueRO.TargetStation;
                var servingData = SystemAPI.GetComponent<ServingStationComponent>(stationEntity);

                Debug.Log("========================================");
                Debug.Log($"[서빙 시도] 플레이어 -> 스테이션 {stationEntity.Index}");

                // 1. 플레이어가 음식을 들고 있는가?
                if (playerState.ValueRO.IsHoldingItem)
                {
                    Entity heldItemEntity = playerState.ValueRO.HeldItemEntity;
                    var itemData = SystemAPI.GetComponent<ItemComponent>(heldItemEntity);

                    // 2. 맨 앞 손님 찾기 (QueueIndex == 0 인 손님)
                    Entity targetCustomer = Entity.Null;
                    IngredientType orderedDish = IngredientType.None;

                    // (최적화를 위해 DynamicBuffer에 손님 목록을 관리할 수도 있지만, 
                    // 지금은 간단하게 전체 손님 중 해당 줄의 0번을 찾습니다.)
                    foreach (var (customer, customerEntity) in
                             SystemAPI.Query<RefRW<CustomerComponent>>()
                             .WithAll<CustomerTag>()
                             .WithEntityAccess())
                    {
                        // 내 줄에 서 있고, 맨 앞이고, 주문/대기 상태인 손님
                        if (customer.ValueRO.TargetStation == stationEntity &&
                            customer.ValueRO.QueueIndex == 0 &&
                            (customer.ValueRO.State == CustomerState.Ordering ||
                             customer.ValueRO.State == CustomerState.WaitingInQueue || // 대기 중인 상태
                             customer.ValueRO.State == CustomerState.WaitingLate))
                        {
                            targetCustomer = customerEntity;
                            orderedDish = customer.ValueRO.OrderDish;
                            break; // 찾음!
                        }
                    }

                    if (targetCustomer != Entity.Null)
                    {
                        // 3. 주문 일치 확인
                        if (itemData.IngredientType == orderedDish)
                        {
                            // ?? [성공] 서빙 완료!
                            Debug.Log($"[서빙 성공] 손님이 {orderedDish}를 받고 행복해합니다! ??");

                            // A. 음식 삭제 (플레이어 손에서)
                            ecb.DestroyEntity(heldItemEntity);
                            playerState.ValueRW.IsHoldingItem = false;
                            playerState.ValueRW.HeldItemEntity = Entity.Null;

                            // B. 손님 상태 변경 -> Happy Leave
                            // (주의: 여기서 바로 Destroy 하지 않고 상태만 바꿈 -> PatienceSystem이 처리)
                            var customerData = SystemAPI.GetComponent<CustomerComponent>(targetCustomer);
                            customerData.State = CustomerState.Leaving_Happy;
                            customerData.Patience = 0; // 즉시 대기 종료
                                                       // 비주얼 반응을 위해 살짝 딜레이를 줄 수도 있음 (PatienceSystem 로직 따름)

                            ecb.SetComponent(targetCustomer, customerData);

                            // C. 점수/돈 증가 (나중에 추가)
                            if (SystemAPI.TryGetSingletonRW<GameSessionComponent>(out var session))
                            {
                                session.ValueRW.CurrentScore += 100; //여기 하드코딩 수정하기..
                                session.ValueRW.ServedCustomers++;
                                session.ValueRW.ProcessedCount++; // 처리됨!
                            }

                            // D. 대기열 관리는 PatienceSystem의 Leaving 처리에서 자동으로 됨
                            // (DecreaseQueueCount & UpdateQueueIndices)
                        }
                        else
                        {
                            // ? [실패] 잘못된 음식
                            Debug.LogWarning($"[서빙 실패] 손님 주문: {orderedDish} / 가져온 것: {itemData.IngredientType}");
                            // (선택) 손님 인내심을 깎거나, 플레이어가 경직되거나...
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[서빙 실패] 받을 손님이 없습니다! (아직 안 왔거나 집에 감)");
                    }
                }
                else
                {
                    Debug.LogWarning("[서빙 실패] 빈손으로 서빙할 수 없습니다.");
                }

                Debug.Log("========================================");

                // [마무리] 요청 제거
                ecb.RemoveComponent<InteractionRequestComponent>(entity);
                ecb.RemoveComponent<ServingRequestTag>(entity);
            }
        }
    }
}