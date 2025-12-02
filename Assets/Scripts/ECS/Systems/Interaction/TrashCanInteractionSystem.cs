using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// 쓰레기통 상호작용 시스템
    /// 역할: 플레이어가 들고 있는 아이템을 제거(Destroy)함
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InteractionSystem))]
    public partial class TrashCanInteractionSystem : SystemBase
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

            // "쓰레기통 태그(TrashCanRequestTag)"가 붙은 플레이어만 필터링
            foreach (var (request, playerState, entity) in
                     SystemAPI.Query<RefRO<InteractionRequestComponent>, RefRW<PlayerStateComponent>>()
                         .WithAll<TrashCanRequestTag>()
                         .WithEntityAccess())
            {
                // [방어 코드] 조합 시스템 등에 의해 이미 처리되었으면 패스
                if (request.ValueRO.TargetStation == Entity.Null)
                {
                    ecb.RemoveComponent<InteractionRequestComponent>(entity);
                    ecb.RemoveComponent<TrashCanRequestTag>(entity);
                    continue;
                }

                Debug.Log("========================================");

                // 플레이어가 아이템을 들고 있는가?
                if (playerState.ValueRO.IsHoldingItem)
                {
                    Entity heldItem = playerState.ValueRO.HeldItemEntity;

                    // 1. 아이템 파괴 (ECS 세계에서 삭제)
                    // VisualSystem이 이걸 감지해서 자동으로 GameObject도 반납함
                    if (heldItem != Entity.Null)
                    {
                        // 어떤 아이템이었는지 로그용 (선택)
                        // var itemData = SystemAPI.GetComponent<ItemComponent>(heldItem);
                        // Debug.Log($"[쓰레기통] {itemData.IngredientType} 버림!");

                        ecb.DestroyEntity(heldItem);
                    }

                    // 2. 플레이어 손 비우기
                    playerState.ValueRW.IsHoldingItem = false;
                    playerState.ValueRW.HeldItemEntity = Entity.Null;

                    Debug.Log("[성공] 쓰레기통에 아이템을 버렸습니다. (속이 다 시원하네요!)");
                }
                else
                {
                    Debug.LogWarning("[실패] 버릴 아이템이 없습니다! (빈손)");
                }

                Debug.Log("========================================");

                // [마무리] 요청 처리 완료 -> 포스트잇 떼기
                ecb.RemoveComponent<InteractionRequestComponent>(entity);
                ecb.RemoveComponent<TrashCanRequestTag>(entity);
            }
        }
    }
}