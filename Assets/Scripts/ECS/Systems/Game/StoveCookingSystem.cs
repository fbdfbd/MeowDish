using Unity.Entities;
using UnityEngine;
using Meow.ECS.Components;
using Meow.Managers;

namespace Meow.ECS.Systems
{

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class StoveCookingSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        private void StopLoop(RefRW<StoveCookingState> stoveState)
        {
            if (stoveState.ValueRO.SfxLoopHandle != 0 && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopLoop(stoveState.ValueRO.SfxLoopHandle);
                stoveState.ValueRW.SfxLoopHandle = 0;
            }
        }

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            if (SystemAPI.TryGetSingleton<GamePauseComponent>(out var pause))
            {
                if (pause.IsPaused) return;
            }

            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (stoveState, stoveData, stoveEntity) in
                     SystemAPI.Query<RefRW<StoveCookingState>, RefRO<StoveComponent>>()
                         .WithEntityAccess())
            {
                if (!stoveState.ValueRO.IsCooking)
                {
                    StopLoop(stoveState);
                    continue;
                }
                if (stoveState.ValueRO.ItemEntity == Entity.Null || !SystemAPI.Exists(stoveState.ValueRO.ItemEntity))
                {
                    StopLoop(stoveState);
                    stoveState.ValueRW.IsCooking = false;
                    continue;
                }

                Entity itemEntity = stoveState.ValueRO.ItemEntity;

                if (!SystemAPI.HasComponent<CookableComponent>(itemEntity))
                {
                    StopLoop(stoveState);
                    stoveState.ValueRW.IsCooking = false;
                    continue;
                }

                var cookable = SystemAPI.GetComponent<CookableComponent>(itemEntity);
                var item = SystemAPI.GetComponent<ItemComponent>(itemEntity);

                float progress = stoveState.ValueRO.CurrentCookProgress;
                if (SystemAPI.HasComponent<CookingState>(itemEntity))
                {
                    progress = SystemAPI.GetComponent<CookingState>(itemEntity).Elapsed;
                }

                progress += deltaTime * stoveData.ValueRO.CookingSpeedMultiplier;
                stoveState.ValueRW.CurrentCookProgress = progress;

                // 아이템에 진행도 동기화
                if (SystemAPI.HasComponent<CookingState>(itemEntity))
                {
                    ecb.SetComponent(itemEntity, new CookingState { Elapsed = progress });
                }
                else
                {
                    ecb.AddComponent(itemEntity, new CookingState { Elapsed = progress });
                }

                // Raw > Cooked
                if (item.State == ItemState.Raw && progress >= cookable.CookTime)
                {
                    ecb.SetComponent(itemEntity, new ItemComponent
                    {
                        ItemID = item.ItemID,
                        Type = item.Type,
                        IngredientType = item.IngredientType,
                        State = ItemState.Cooked
                    });

                    ecb.RemoveComponent<RawItemTag>(itemEntity);
                    ecb.AddComponent<CookedItemTag>(itemEntity);

                    Debug.Log($"[Stove] {item.IngredientType}가 Cooked");
                }

                // Cooked > Burnt
                if (cookable.BurnTime > 0 && item.State == ItemState.Cooked && progress >= (cookable.CookTime + cookable.BurnTime))
                {
                    if (SystemAPI.HasComponent<BurnableTag>(itemEntity))
                    {
                        ecb.SetComponent(itemEntity, new ItemComponent
                        {
                            ItemID = item.ItemID,
                            Type = item.Type,
                            IngredientType = item.IngredientType,
                            State = ItemState.Burnt
                        });

                        ecb.RemoveComponent<CookedItemTag>(itemEntity);
                        ecb.AddComponent<BurnedItemTag>(itemEntity);

                        Debug.Log($"[Stove] {item.IngredientType}가 탔습니다");

                        StopLoop(stoveState);
                        stoveState.ValueRW.IsCooking = false;
                    }
                }
            }
        }
    }
}
