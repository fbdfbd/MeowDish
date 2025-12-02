using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections; // NativeArray 사용을 위해 필수
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InteractionSystem))]
    [UpdateBefore(typeof(ContainerInteractionSystem))]
    [UpdateBefore(typeof(CounterInteractionSystem))]
    [UpdateBefore(typeof(StoveInteractionSystem))]
    public partial class ItemCombinationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var em = EntityManager;

            // =========================================================================
            // 1. 대상자 스냅샷 찍기 (QueryBuilder 사용)
            // =========================================================================
            // foreach 문 안에서 구조적 변경을 하기 위해, 먼저 Entity 목록만 따로 빼냅니다.
            EntityQuery query = SystemAPI.QueryBuilder()
                .WithAll<InteractionRequestComponent, PlayerStateComponent, LocalTransform>()
                .Build();

            // 대상자가 없으면 리턴 (성능 최적화)
            if (query.IsEmptyIgnoreFilter) return;

            // 엔티티 배열로 복사 (Allocator.Temp는 이 함수 끝나면 자동 해제됨)
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

            // =========================================================================
            // 2. 명단 순회하며 로직 수행
            // =========================================================================
            // 이제 SystemAPI.Query가 아니라, 복사해둔 배열(entities)을 돕니다.
            // 여기서는 EntityManager를 마음껏 써서 지지고 볶아도 안전합니다!
            foreach (var entity in entities)
            {
                // 엔티티가 그새 죽었을 수도 있으니 방어 코드
                if (!em.Exists(entity)) continue;

                // 데이터 가져오기 (RefRW 대신 직접 읽어옴)
                var playerState = em.GetComponentData<PlayerStateComponent>(entity);
                var request = em.GetComponentData<InteractionRequestComponent>(entity);
                var playerTransform = em.GetComponentData<LocalTransform>(entity);

                // --- [기존 로직 시작] ---

                // 1. 플레이어가 아이템을 안 들고 있으면 패스
                if (!playerState.IsHoldingItem) continue;

                Entity heldItemEntity = playerState.HeldItemEntity;
                if (!em.Exists(heldItemEntity) || !em.HasComponent<ItemComponent>(heldItemEntity)) continue;

                var heldItemData = em.GetComponentData<ItemComponent>(heldItemEntity);
                Entity targetStation = request.TargetStation;

                // 2. 대상 스테이션에서 재료 확인
                ItemComponent targetItemData = new ItemComponent { IngredientType = IngredientType.None };
                Entity targetItemEntity = Entity.Null;
                bool isTargetRealEntity = false;

                // A. 컨테이너
                if (em.HasComponent<ContainerComponent>(targetStation))
                {
                    var container = em.GetComponentData<ContainerComponent>(targetStation);
                    targetItemData.IngredientType = container.ProvidedIngredient;
                    targetItemData.State = ItemState.Raw;
                }
                // B. 카운터
                else if (em.HasComponent<CounterComponent>(targetStation))
                {
                    var buffer = em.GetBuffer<CounterItemSlot>(targetStation);
                    if (buffer.Length > 0)
                    {
                        targetItemEntity = buffer[buffer.Length - 1].ItemEntity;
                        if (em.Exists(targetItemEntity))
                        {
                            targetItemData = em.GetComponentData<ItemComponent>(targetItemEntity);
                            isTargetRealEntity = true;
                        }
                    }
                }
                // C. 스토브
                else if (em.HasComponent<StoveCookingState>(targetStation))
                {
                    var stoveState = em.GetComponentData<StoveCookingState>(targetStation);
                    if (stoveState.ItemEntity != Entity.Null && em.Exists(stoveState.ItemEntity))
                    {
                        targetItemEntity = stoveState.ItemEntity;
                        targetItemData = em.GetComponentData<ItemComponent>(targetItemEntity);
                        isTargetRealEntity = true;
                    }
                }

                if (targetItemData.IngredientType == IngredientType.None) continue;

                // 3. 레시피 검사
                IngredientType resultType = CheckRecipe(heldItemData, targetItemData);

                // 4. 조합 성공! (즉시 실행 - 이제 에러 안 남!)
                if (resultType != IngredientType.None)
                {
                    Debug.Log($"[조합 시스템] {heldItemData.IngredientType} + {targetItemData.IngredientType} = {resultType}");

                    // A. 재료 소모
                    em.DestroyEntity(heldItemEntity);

                    if (isTargetRealEntity && targetItemEntity != Entity.Null)
                    {
                        em.DestroyEntity(targetItemEntity);

                        // 스테이션 데이터 정리
                        if (em.HasComponent<CounterComponent>(targetStation))
                        {
                            var buffer = em.GetBuffer<CounterItemSlot>(targetStation);
                            if (buffer.Length > 0) buffer.RemoveAt(buffer.Length - 1);
                        }
                        if (em.HasComponent<StoveCookingState>(targetStation))
                        {
                            var stoveState = em.GetComponentData<StoveCookingState>(targetStation);
                            stoveState.ItemEntity = Entity.Null;
                            stoveState.IsCooking = false;
                            stoveState.CurrentCookProgress = 0;
                            em.SetComponentData(targetStation, stoveState);
                        }
                    }

                    // B. 결과물 생성
                    Entity resultItem = em.CreateEntity();
                    em.AddComponentData(resultItem, new ItemComponent
                    {
                        ItemID = heldItemData.ItemID,
                        Type = ItemType.Dish,
                        State = ItemState.Raw,
                        IngredientType = resultType
                    });

                    em.AddComponentData(resultItem, new HoldableComponent { HolderEntity = entity });

                    float3 handPos = playerTransform.Position + new float3(0, 1.5f, 0.5f);
                    em.AddComponentData(resultItem, new LocalTransform { Position = handPos, Rotation = quaternion.identity, Scale = 1f });

                    // 태그 추가
                    em.AddComponent<DishTag>(resultItem);

                    // C. 플레이어 손 갱신
                    playerState.HeldItemEntity = resultItem;
                    // 값 타입이므로 다시 SetComponentData 해줘야 저장됨!
                    em.SetComponentData(entity, playerState);

                    // ?? [핵심] 요청 즉시 삭제!
                    em.RemoveComponent<InteractionRequestComponent>(entity);

                    // 태그 정리
                    if (em.HasComponent<ContainerRequestTag>(entity)) em.RemoveComponent<ContainerRequestTag>(entity);
                    if (em.HasComponent<CounterRequestTag>(entity)) em.RemoveComponent<CounterRequestTag>(entity);
                    if (em.HasComponent<StoveRequestTag>(entity)) em.RemoveComponent<StoveRequestTag>(entity);
                    if (em.HasComponent<CuttingBoardRequestTag>(entity)) em.RemoveComponent<CuttingBoardRequestTag>(entity);
                }
            }

            // NativeArray는 Allocator.Temp라서 프레임 끝나면 자동 해제되지만, 명시적으로 Dispose 해줘도 됨.
            entities.Dispose();
        }

        private IngredientType CheckRecipe(ItemComponent held, ItemComponent target)
        {
            // 1. [버거 만들기]
            if (held.IngredientType == IngredientType.Bread &&
                target.IngredientType == IngredientType.Meat && target.State == ItemState.Cooked)
                return IngredientType.Burger;

            if (held.IngredientType == IngredientType.Meat && held.State == ItemState.Cooked &&
                target.IngredientType == IngredientType.Bread)
                return IngredientType.Burger;

            // 2. [포장하기]
            if (held.IngredientType == IngredientType.Wrapper &&
                target.IngredientType == IngredientType.Burger)
                return IngredientType.WrappedBurger;

            if (held.IngredientType == IngredientType.Burger &&
                target.IngredientType == IngredientType.Wrapper)
                return IngredientType.WrappedBurger;

            return IngredientType.None;
        }
    }
}