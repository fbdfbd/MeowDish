using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InteractionSystem))]
    public partial class StoveInteractionSystem : SystemBase
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

            // "스토브 요청 태그(StoveRequestTag)"가 붙은 플레이어만 필터링
            foreach (var (request, playerState, playerTransform, entity) in
                     SystemAPI.Query<RefRO<InteractionRequestComponent>, RefRW<PlayerStateComponent>, RefRO<LocalTransform>>()
                         .WithAll<StoveRequestTag>()
                         .WithEntityAccess())
            {
                Entity stoveEntity = request.ValueRO.TargetStation;

                var stoveData = SystemAPI.GetComponent<StoveComponent>(stoveEntity);
                var stoveState = SystemAPI.GetComponentRW<StoveCookingState>(stoveEntity);
                var stoveSnap = SystemAPI.GetComponent<StoveSnapPoint>(stoveEntity);
                var stoveTransform = SystemAPI.GetComponent<LocalTransform>(stoveEntity);

                Debug.Log("========================================");

                // ============================================================
                // CASE 1: 플레이어 -> 스토브 (올리기)
                // ============================================================
                if (playerState.ValueRO.IsHoldingItem)
                {
                    // 1. 스토브가 이미 사용 중인가?
                    if (stoveState.ValueRO.ItemEntity != Entity.Null)
                    {
                        Debug.LogWarning("[실패] 스토브가 이미 사용 중입니다!");
                    }
                    else
                    {
                        Entity heldItem = playerState.ValueRO.HeldItemEntity;

                        // 2. ?? [검사] 구울 수 있는 아이템인가? (CookableComponent 체크)
                        if (SystemAPI.HasComponent<CookableComponent>(heldItem))
                        {
                            // 스토브 상태 갱신 (올리기)
                            stoveState.ValueRW.ItemEntity = heldItem;
                            stoveState.ValueRW.CurrentCookProgress = 0f;
                            stoveState.ValueRW.IsCooking = true; // 요리 시작!

                            // 위치 이동 (스냅 포인트)
                            float3 worldPos = stoveTransform.Position + math.rotate(stoveTransform.Rotation, stoveSnap.LocalPosition);

                            ecb.SetComponent(heldItem, new LocalTransform
                            {
                                Position = worldPos,
                                Rotation = quaternion.identity,
                                Scale = 1f
                            });

                            // 소유권 이전 (플레이어 -> 스토브)
                            ecb.SetComponent(heldItem, new HoldableComponent { HolderEntity = stoveEntity });

                            // 플레이어 손 비우기
                            playerState.ValueRW.IsHoldingItem = false;
                            playerState.ValueRW.HeldItemEntity = Entity.Null;

                            Debug.Log($"[성공] 스토브에 아이템을 올렸습니다. 조리 시작!");
                        }
                        else
                        {
                            Debug.LogWarning("[실패] 이 아이템은 구울 수 없습니다!");
                        }
                    }
                }
                // ============================================================
                // CASE 2: 스토브 -> 플레이어 (꺼내기)
                // ============================================================
                else
                {
                    // 스토브가 비었는지 확인
                    if (stoveState.ValueRO.ItemEntity == Entity.Null)
                    {
                        Debug.LogWarning("[실패] 스토브가 비어있습니다!");
                    }
                    else
                    {
                        Entity targetItem = stoveState.ValueRO.ItemEntity;

                        // 스토브 상태 갱신 (비우기)
                        stoveState.ValueRW.ItemEntity = Entity.Null;
                        stoveState.ValueRW.IsCooking = false; // 요리 중단
                        stoveState.ValueRW.CurrentCookProgress = 0f;

                        // 위치 이동 (플레이어 손)
                        float3 handPos = playerTransform.ValueRO.Position + new float3(0, 1.5f, 0.5f);

                        ecb.SetComponent(targetItem, new LocalTransform
                        {
                            Position = handPos,
                            Rotation = quaternion.identity,
                            Scale = 1f
                        });

                        // 소유권 이전 (스토브 -> 플레이어)
                        ecb.SetComponent(targetItem, new HoldableComponent { HolderEntity = entity });

                        // 플레이어 손 채우기
                        playerState.ValueRW.IsHoldingItem = true;
                        playerState.ValueRW.HeldItemEntity = targetItem;

                        Debug.Log($"[성공] 스토브에서 아이템을 꺼냈습니다.");
                    }
                }

                Debug.Log("========================================");

                // 처리 완료 (포스트잇 떼기)
                ecb.RemoveComponent<InteractionRequestComponent>(entity);
                ecb.RemoveComponent<StoveRequestTag>(entity);
            }
        }
    }
}