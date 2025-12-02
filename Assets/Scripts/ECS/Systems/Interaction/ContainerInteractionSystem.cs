using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InteractionSystem))]
    public partial class ContainerInteractionSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;
        private int _itemIdCounter = 1000;

        protected override void OnCreate()
        {
            base.OnCreate();
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer();
            int currentItemId = _itemIdCounter;

            // ContainerRequestTag가 붙은 플레이어만 처리
            foreach (var (request, playerState, playerTransform, entity) in
                     SystemAPI.Query<RefRO<InteractionRequestComponent>, RefRW<PlayerStateComponent>, RefRO<LocalTransform>>()
                         .WithAll<ContainerRequestTag>()
                         .WithEntityAccess())
            {
                Entity stationEntity = request.ValueRO.TargetStation;
                var container = SystemAPI.GetComponent<ContainerComponent>(stationEntity);

                Debug.Log("========================================");
                Debug.Log($"[컨테이너 상호작용] 플레이어: {entity.Index} | 재료: {container.ProvidedIngredient}");

                // ==========================================
                // 1. 빈손 → 아이템 꺼내기 (오류 수정된 부분!)
                // ==========================================
                if (!playerState.ValueRO.IsHoldingItem)
                {
                    Debug.Log($"[아이템 꺼내기] {container.ProvidedIngredient}");

                    // 1. 아이템 엔티티 생성 (아직 가짜 ID 상태)
                    Entity newItemEntity = ecb.CreateEntity();

                    // 2. 아이템 컴포넌트 설정
                    ecb.AddComponent(newItemEntity, new ItemComponent
                    {
                        ItemID = currentItemId++,
                        Type = ItemType.Ingredient,
                        State = ItemState.Raw,
                        IngredientType = container.ProvidedIngredient
                    });

                    ecb.AddComponent(newItemEntity, new HoldableComponent
                    {
                        HolderEntity = entity
                    });

                    ecb.AddComponent(newItemEntity, new LocalTransform
                    {
                        Position = playerTransform.ValueRO.Position + new float3(0, 1.5f, 0.5f),
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });

                    ecb.AddComponent<IngredientTag>(newItemEntity);
                    ecb.AddComponent<RawItemTag>(newItemEntity);

                    if (container.ProvidedIngredient == IngredientType.Meat)
                    {
                        ecb.AddComponent(newItemEntity, new CookableComponent { CookTime = 5f, BurnTime = 8f });
                        ecb.AddComponent<BurnableTag>(newItemEntity);
                    }

                    // ?? [수정 완료] PlayerState 갱신을 ECB로 변경 ??
                    // CreateEntity로 만든 건 아직 진짜가 아니라서, 직접 대입하면 에러남!
                    // 복사본을 만들어서 ECB에게 "나중에 진짜 ID 나오면 넣어줘"라고 부탁해야 함.
                    var newState = playerState.ValueRO;
                    newState.IsHoldingItem = true;
                    newState.HeldItemEntity = newItemEntity; // 가짜 ID 넣음

                    ecb.SetComponent(entity, newState); // ECB가 나중에 진짜 ID로 변환해줌

                    Debug.Log($"[성공] {container.ProvidedIngredient} 획득! (ItemID: {currentItemId - 1})");
                }
                // ==========================================
                // 2. 아이템 들고있음 → 반납 시도
                // ==========================================
                else
                {
                    Entity heldItemEntity = playerState.ValueRO.HeldItemEntity;

                    if (heldItemEntity == Entity.Null)
                    {
                        Debug.LogWarning("[오류] IsHoldingItem=true인데 HeldItemEntity가 Null!");
                    }
                    else if (!SystemAPI.HasComponent<ItemComponent>(heldItemEntity))
                    {
                        Debug.LogWarning("[오류] HeldItemEntity에 ItemComponent가 없음!");
                    }
                    else
                    {
                        var heldItem = SystemAPI.GetComponent<ItemComponent>(heldItemEntity);

                        Debug.Log($"[반납 시도] 들고있는 아이템: {heldItem.IngredientType} (상태: {heldItem.State})");

                        bool isMatchingType = heldItem.IngredientType == container.ProvidedIngredient;
                        bool isRawState = heldItem.State == ItemState.Raw;
                        bool canReturn = container.AllowReturn;

                        if (isMatchingType && isRawState && canReturn)
                        {
                            ecb.DestroyEntity(heldItemEntity);

                            // 여기는 Entity.Null(이미 존재하는 값)을 넣는 거라 직접 대입해도 안전함
                            playerState.ValueRW.IsHoldingItem = false;
                            playerState.ValueRW.HeldItemEntity = Entity.Null;

                            Debug.Log($"[성공] {heldItem.IngredientType} 반납 완료!");
                        }
                        else
                        {
                            if (!canReturn) Debug.Log("[실패] 반납 불가 컨테이너");
                            else if (!isMatchingType) Debug.Log($"[실패] 타입 불일치 ({container.ProvidedIngredient}만 가능)");
                            else if (!isRawState) Debug.Log($"[실패] Raw 상태만 반납 가능 (현재: {heldItem.State})");
                        }
                    }
                }

                Debug.Log("========================================");

                // [마무리] 요청 처리 완료 -> 포스트잇 떼기
                ecb.RemoveComponent<InteractionRequestComponent>(entity);
                ecb.RemoveComponent<ContainerRequestTag>(entity);
            }

            _itemIdCounter = currentItemId;
        }
    }
}