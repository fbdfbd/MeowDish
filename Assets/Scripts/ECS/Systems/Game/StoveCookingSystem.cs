using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// 스토브 위에서 아이템을 굽는 시스템
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class StoveCookingSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = _ecbSystem.CreateCommandBuffer();

            // "요리 중(IsCooking = true)"인 스토브만 찾습니다.
            foreach (var (stoveState, stoveData, stoveEntity) in
                     SystemAPI.Query<RefRW<StoveCookingState>, RefRO<StoveComponent>>()
                         .WithEntityAccess())
            {
                if (!stoveState.ValueRO.IsCooking) continue;
                if (stoveState.ValueRO.ItemEntity == Entity.Null) continue;

                Entity itemEntity = stoveState.ValueRO.ItemEntity;

                // 아이템이 유효한지 확인
                if (!SystemAPI.HasComponent<CookableComponent>(itemEntity))
                {
                    // 예외 처리: 구울 수 없는게 올라가 있음 -> 요리 중단
                    stoveState.ValueRW.IsCooking = false;
                    continue;
                }

                var cookable = SystemAPI.GetComponent<CookableComponent>(itemEntity);
                var item = SystemAPI.GetComponent<ItemComponent>(itemEntity); // 상태 확인용 (ReadOnly 아님, 복사본)

                // 1. 시간 진행
                // 스토브 속도 * 델타타임
                float progress = stoveState.ValueRO.CurrentCookProgress + (deltaTime * stoveData.ValueRO.CookingSpeedMultiplier);
                stoveState.ValueRW.CurrentCookProgress = progress;

                // 2. 상태 변화 체크

                // [Raw -> Cooked] 익음 판정
                if (item.State == ItemState.Raw && progress >= cookable.CookTime)
                {
                    // 상태 변경
                    ecb.SetComponent(itemEntity, new ItemComponent
                    {
                        ItemID = item.ItemID,
                        Type = item.Type,
                        IngredientType = item.IngredientType,
                        State = ItemState.Cooked // ?? 익었다!
                    });

                    // 태그 교체 (Raw 태그 떼고 Cooked 태그 붙이기) -> 쿼리 최적화용
                    ecb.RemoveComponent<RawItemTag>(itemEntity);
                    ecb.AddComponent<CookedItemTag>(itemEntity);

                    Debug.Log($"[Stove] {item.IngredientType}가 맛있게 익었습니다!");
                }

                // [Cooked -> Burnt] 탐 판정
                // BurnTime이 0보다 클 때만 체크
                if (cookable.BurnTime > 0 && item.State == ItemState.Cooked && progress >= (cookable.CookTime + cookable.BurnTime))
                {
                    // 태울 수 있는 아이템인지 확인 (BurnableTag)
                    if (SystemAPI.HasComponent<BurnableTag>(itemEntity))
                    {
                        ecb.SetComponent(itemEntity, new ItemComponent
                        {
                            ItemID = item.ItemID,
                            Type = item.Type,
                            IngredientType = item.IngredientType,
                            State = ItemState.Burnt // ?? 탔다!
                        });

                        ecb.RemoveComponent<CookedItemTag>(itemEntity);
                        ecb.AddComponent<BurnedItemTag>(itemEntity);

                        // 탄 이후에는 요리 진행 멈춤 (선택 사항)
                        // stoveState.ValueRW.IsCooking = false; 

                        Debug.Log($"[Stove] 으악! {item.IngredientType}가 타버렸습니다!");
                    }
                }
            }
        }
    }
}