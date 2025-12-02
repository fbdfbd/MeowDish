using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// 카운터(작업대) 상호작용 시스템
    /// 
    /// 역할:
    /// 1. 플레이어 -> 카운터: 아이템 놓기 (Push & 소유권 이전)
    /// 2. 카운터 -> 플레이어: 아이템 집기 (Pop & 소유권 복구)
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InteractionSystem))]
    public partial class CounterInteractionSystem : SystemBase
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

            // "카운터 요청 태그(CounterRequestTag)"가 붙은 플레이어만 필터링
            foreach (var (request, playerState, playerTransform, entity) in
                     SystemAPI.Query<RefRO<InteractionRequestComponent>, RefRW<PlayerStateComponent>, RefRO<LocalTransform>>()
                         .WithAll<CounterRequestTag>()
                         .WithEntityAccess())
            {
                // 1. 대상 카운터 정보 가져오기
                Entity counterEntity = request.ValueRO.TargetStation;

                // 카운터 관련 데이터 조회
                var counterData = SystemAPI.GetComponent<CounterComponent>(counterEntity);
                var itemBuffer = SystemAPI.GetBuffer<CounterItemSlot>(counterEntity);
                var snapBuffer = SystemAPI.GetBuffer<CounterSnapPoint>(counterEntity);
                var counterTransform = SystemAPI.GetComponent<LocalTransform>(counterEntity);

                Debug.Log("========================================");
                Debug.Log($"[카운터 상호작용] 현재 아이템: {itemBuffer.Length} / {counterData.MaxItems}");

                // ============================================================
                // CASE 1: 플레이어 -> 카운터 (놓기)
                // ============================================================
                if (playerState.ValueRO.IsHoldingItem)
                {
                    // 카운터가 꽉 찼는지 확인
                    if (itemBuffer.Length >= counterData.MaxItems)
                    {
                        Debug.LogWarning("[실패] 카운터가 꽉 찼습니다!");
                    }
                    else
                    {
                        Entity heldItem = playerState.ValueRO.HeldItemEntity;

                        // 1. 카운터 버퍼에 등록 (Stack Push)
                        itemBuffer.Add(new CounterItemSlot { ItemEntity = heldItem });

                        // 2. 아이템 위치 이동 (스냅 포인트 계산)
                        int slotIndex = itemBuffer.Length - 1;
                        float3 snapLocalPos = float3.zero;

                        // 스냅 포인트가 있으면 사용, 없으면 중앙 (0, 1, 0)
                        if (slotIndex < snapBuffer.Length)
                        {
                            snapLocalPos = snapBuffer[slotIndex].LocalPosition;
                        }
                        else
                        {
                            Debug.LogWarning($"[주의] 슬롯 인덱스({slotIndex})에 해당하는 SnapPoint가 없습니다! 중앙에 둡니다.");
                            snapLocalPos = new float3(0, 1.0f, 0);
                        }

                        // 카운터의 회전(Rotation)을 반영하여 월드 좌표 계산
                        float3 worldPos = counterTransform.Position + math.rotate(counterTransform.Rotation, snapLocalPos);

                        // 위치 설정 (이 값을 ItemVisualSystem이 따라감)
                        ecb.SetComponent(heldItem, new LocalTransform
                        {
                            Position = worldPos,
                            Rotation = quaternion.identity, // 아이템은 정방향
                            Scale = 1f
                        });

                        // ?? [핵심 수정] 소유권 이전 (플레이어 -> 카운터)
                        // 이걸 해줘야 ItemVisualSystem이 "어? 주인이 플레이어가 아니네?" 하고 머리 위로 안 가져감.
                        ecb.SetComponent(heldItem, new HoldableComponent
                        {
                            HolderEntity = counterEntity
                        });

                        // 3. 플레이어 손 비우기
                        playerState.ValueRW.IsHoldingItem = false;
                        playerState.ValueRW.HeldItemEntity = Entity.Null;

                        Debug.Log($"[성공] 아이템을 카운터 슬롯 {slotIndex}번에 놓았습니다.");
                    }
                }
                // ============================================================
                // CASE 2: 카운터 -> 플레이어 (가져오기 - LIFO)
                // ============================================================
                else
                {
                    // 카운터가 비었는지 확인
                    if (itemBuffer.Length <= 0)
                    {
                        Debug.LogWarning("[실패] 카운터가 비어있습니다!");
                    }
                    else
                    {
                        // 1. 마지막 아이템 꺼내기 (Stack Pop)
                        int lastIndex = itemBuffer.Length - 1;
                        Entity targetItem = itemBuffer[lastIndex].ItemEntity;

                        // 버퍼에서 제거
                        itemBuffer.RemoveAt(lastIndex);

                        // 2. 플레이어 손으로 이동 (위치 설정)
                        float3 handPos = playerTransform.ValueRO.Position + new float3(0, 1.5f, 0.5f);

                        ecb.SetComponent(targetItem, new LocalTransform
                        {
                            Position = handPos,
                            Rotation = quaternion.identity,
                            Scale = 1f
                        });

                        // ?? [핵심 수정] 소유권 복구 (카운터 -> 플레이어)
                        // 이제 다시 플레이어 머리 위를 따라다니게 됨.
                        ecb.SetComponent(targetItem, new HoldableComponent
                        {
                            HolderEntity = entity // entity = 플레이어
                        });

                        // 3. 플레이어 상태 갱신 (손에 쥐기)
                        playerState.ValueRW.IsHoldingItem = true;
                        playerState.ValueRW.HeldItemEntity = targetItem;

                        Debug.Log($"[성공] 카운터에서 아이템을 집었습니다. (남은 개수: {itemBuffer.Length})");
                    }
                }

                Debug.Log("========================================");

                // ?? [마무리] 처리 완료! 포스트잇 떼기
                ecb.RemoveComponent<InteractionRequestComponent>(entity);
                ecb.RemoveComponent<CounterRequestTag>(entity);
            }
        }
    }
}