using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Meow.ECS.Components;
using Meow.Managers;

namespace Meow.ECS.View
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class ItemVisualSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // 매니저(싱글톤)가 준비 안 됐으면 패스
            if (ItemVisualManager.Instance == null) return;

            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            // ==================================================================
            // 1. 생성 (Spawn)
            // ==================================================================
            foreach (var (item, transform, entity) in
                     SystemAPI.Query<RefRO<ItemComponent>, RefRO<LocalTransform>>()
                     .WithNone<ItemVisualReference>()
                     .WithEntityAccess())
            {
                // [수정됨] 매니저에게 요청해서 프리팹 가져오기
                // transform.ValueRO.Rotation 대신 quaternion.identity를 넘겨줍니다.
                // 이렇게 하면 ECS 엔티티가 굴러가고 있어도, 비주얼은 정자세(0,0,0) 혹은 프리팹 설정대로 생성됩니다.
                GameObject visual = ItemVisualManager.Instance.SpawnItem(
                    item.ValueRO.IngredientType,
                    transform.ValueRO.Position,
                    quaternion.identity
                );

                if (visual != null)
                {
                    // 생성 시 초기 색상 설정
                    UpdateVisualColor(visual, item.ValueRO.IngredientType, item.ValueRO.State);

                    // 명찰 붙이기
                    ecb.AddComponent(entity, new ItemVisualReference
                    {
                        VisualObject = visual,
                        Type = item.ValueRO.IngredientType,
                        LastState = item.ValueRO.State
                    });
                }
            }

            // ==================================================================
            // 2. 동기화 (Sync)
            // ==================================================================
            foreach (var (transform, visualRef, item, entity) in
                     SystemAPI.Query<RefRO<LocalTransform>, ItemVisualReference, RefRO<ItemComponent>>()
                     .WithEntityAccess())
            {
                if (visualRef.VisualObject != null)
                {
                    Transform visTransform = visualRef.VisualObject.transform;
                    float3 targetPos = transform.ValueRO.Position;

                    // 기본적으로 회전 동기화를 하지 않음 (프리팹 본연의 회전 유지)
                    // quaternion targetRot = transform.ValueRO.Rotation; // <--- 이 부분이 삭제되었습니다.

                    bool isHeldByPlayer = false;
                    quaternion holdRotation = quaternion.identity;

                    // [위치 보정] 플레이어가 들고 있을 때만 머리 위로 강제 이동 및 회전 동기화
                    if (SystemAPI.HasComponent<HoldableComponent>(entity))
                    {
                        var holdable = SystemAPI.GetComponent<HoldableComponent>(entity);
                        if (holdable.HolderEntity != Entity.Null &&
                            SystemAPI.Exists(holdable.HolderEntity) &&
                            SystemAPI.HasComponent<PlayerTag>(holdable.HolderEntity))
                        {
                            var playerTransform = SystemAPI.GetComponent<LocalTransform>(holdable.HolderEntity);
                            targetPos = playerTransform.Position + new float3(0, 1.0f, 0.5f);

                            // 플레이어가 들고 있을 때는 플레이어 방향을 따라가야 자연스러움
                            holdRotation = playerTransform.Rotation;
                            isHeldByPlayer = true;
                        }
                    }

                    // 위치는 항상 ECS 좌표(혹은 들고 있는 위치)를 따라감
                    visTransform.position = targetPos;

                    // [수정됨] 회전은 '플레이어가 들고 있을 때'만 강제로 변경하고,
                    // 그 외(바닥에 있거나 컨베이어 위)일 때는 건드리지 않음.
                    if (isHeldByPlayer)
                    {
                        visTransform.rotation = holdRotation;
                    }

                    // [상태 보정] 상태가 변했으면 색상 업데이트 (최적화)
                    if (visualRef.LastState != item.ValueRO.State)
                    {
                        UpdateVisualColor(visualRef.VisualObject, item.ValueRO.IngredientType, item.ValueRO.State);
                        visualRef.LastState = item.ValueRO.State;
                    }
                }
            }

            // ==================================================================
            // 3. 반납 (Despawn)
            // ==================================================================
            foreach (var (visualRef, entity) in
                     SystemAPI.Query<ItemVisualReference>()
                     .WithNone<ItemComponent>()
                     .WithEntityAccess())
            {
                if (visualRef.VisualObject != null)
                {
                    // 반납 시 색상 초기화 (다음 사용을 위해 Raw 상태 색으로 복구)
                    UpdateVisualColor(visualRef.VisualObject, visualRef.Type, ItemState.Raw);

                    // 매니저에게 반납
                    ItemVisualManager.Instance.ReturnItem(visualRef.Type, visualRef.VisualObject);
                }

                // 청소부 컴포넌트 삭제
                ecb.RemoveComponent<ItemVisualReference>(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        // ==================================================================
        // ?? 색상 변경 로직 (Bread vs Meat 구분 추가됨)
        // ==================================================================
        private void UpdateVisualColor(GameObject visual, IngredientType type, ItemState state)
        {
            // 완성품(버거, 포장지 등)은 프리팹 색상 그대로 유지
            if (type == IngredientType.Burger ||
                type == IngredientType.Wrapper ||
                type == IngredientType.WrappedBurger)
                return;

            var renderer = visual.GetComponent<Renderer>();
            if (renderer == null) return;

            switch (state)
            {
                case ItemState.Raw:
                    if (type == IngredientType.Bread)
                    {
                        // 빵: 맛있는 베이지색
                        // renderer.material.color = new Color(1.0f, 0.85f, 0.6f);
                    }
                    else if (type == IngredientType.Meat)
                    {
                        // 고기: 선홍색 (생고기)
                        renderer.material.color = new Color(1.0f, 0.6f, 0.6f);
                    }
                    else
                    {
                        // 그 외: 기본 흰색
                        renderer.material.color = Color.white;
                    }
                    break;

                case ItemState.Cooked:
                    if (type == IngredientType.Meat)
                        renderer.material.color = new Color(0.6f, 0.3f, 0.1f); // 갈색 (익은 고기)
                    break;

                case ItemState.Burnt:
                    renderer.material.color = new Color(0.2f, 0.2f, 0.2f); // 검은색 (탐)
                    break;
            }
        }
    }
}