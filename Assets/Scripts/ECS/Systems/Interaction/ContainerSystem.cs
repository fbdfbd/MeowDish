using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// 컨테이너 상호작용 처리
    /// 
    /// 기능:
    /// 1. 빈손 + E키 = 재료 꺼내기
    /// 2. 아이템 들고 + E키 = 반납 시도 (Raw 상태만)
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InteractionSystem))]
    public partial class ContainerSystem : SystemBase
    {
        private int _nextItemID = 1;

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            Entities.ForEach((
                Entity playerEntity,
                ref PlayerStateComponent playerState,
                in PlayerInputComponent input,
                in LocalTransform playerTransform) =>
            {
                if (!input.InteractPressed || !playerState.IsNearStation)
                    return;

                if (!SystemAPI.HasComponent<ContainerComponent>(playerState.CurrentStationEntity))
                    return;

                var container = SystemAPI.GetComponent<ContainerComponent>(playerState.CurrentStationEntity);

                // ????????????????????????????????????????
                // Case 1: 빈손 → 재료 꺼내기
                // ????????????????????????????????????????
                if (!playerState.IsHoldingItem)
                {
                    TakeItemFromContainer(
                        ecb,
                        playerEntity,
                        ref playerState,
                        container.ProvidedIngredient,  // ?? StationID 제거!
                        playerTransform.Position
                    );
                }
                // ????????????????????????????????????????
                // Case 2: 아이템 들고 있음 → 반납 시도
                // ????????????????????????????????????????
                else if (container.AllowReturn)
                {
                    TryReturnItemToContainer(
                        ecb,
                        playerEntity,
                        ref playerState,
                        playerState.CurrentStationEntity  // ?? StationID 대신 Entity 전달
                    );
                }

            }).WithoutBurst().Run();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// 컨테이너에서 아이템 꺼내기
        /// </summary>
        /// <summary>
        /// 컨테이너에서 아이템 꺼내기
        /// </summary>
        private void TakeItemFromContainer(
            EntityCommandBuffer ecb,
            Entity playerEntity,
            ref PlayerStateComponent playerState,
            IngredientType ingredientType,
            float3 playerPos)
        {
            Entity newItem = ecb.CreateEntity();

            ecb.AddComponent(newItem, new ItemComponent
            {
                ItemID = _nextItemID++,
                Type = ItemType.Ingredient,
                State = ItemState.Raw,
                IngredientType = ingredientType
            });

            ecb.AddComponent(newItem, new HoldableComponent
            {
                HolderEntity = playerEntity
            });

            ecb.AddComponent(newItem, LocalTransform.FromPosition(playerPos + new float3(0, 1, 0)));

            playerState.HeldItemEntity = newItem;
            playerState.IsHoldingItem = true;

            Debug.Log($"[ContainerSystem] Took {ingredientType}");
        }

        /// <summary>
        /// 컨테이너에 아이템 반납 시도
        /// </summary>
        /// <summary>
        /// 컨테이너에 아이템 반납 시도
        /// </summary>
        private void TryReturnItemToContainer(
            EntityCommandBuffer ecb,
            Entity playerEntity,
            ref PlayerStateComponent playerState,
            Entity containerEntity)
        {
            var item = SystemAPI.GetComponent<ItemComponent>(playerState.HeldItemEntity);
            var container = SystemAPI.GetComponent<ContainerComponent>(containerEntity);

            // ????????????????????????????????????????
            // 반납 조건 검증 (간단!)
            // ????????????????????????????????????????

            // 1. 같은 재료 타입인가?
            if (item.IngredientType != container.ProvidedIngredient)
            {
                Debug.Log("[ContainerSystem] Return failed: Different ingredient type!");
                return;
            }

            // 2. 상태가 Raw (가공 안 됨)인가?
            if (item.State != ItemState.Raw)
            {
                Debug.Log("[ContainerSystem] Return failed: Item is processed!");
                return;
            }

            // ????????????????????????????????????????
            // 반납 성공!
            // ????????????????????????????????????????

            ecb.DestroyEntity(playerState.HeldItemEntity);

            playerState.HeldItemEntity = Entity.Null;
            playerState.IsHoldingItem = false;

            Debug.Log($"[ContainerSystem] Returned {item.IngredientType}");
        }
    }
}