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
        private MaterialPropertyBlock _propBlock;

        protected override void OnCreate()
        {
            base.OnCreate();
            _propBlock = new MaterialPropertyBlock();
        }

        protected override void OnUpdate()
        {
            if (ItemVisualManager.Instance == null) return;

            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (item, transform, entity) in
                     SystemAPI.Query<RefRO<ItemComponent>, RefRO<LocalTransform>>()
                     .WithNone<ItemVisualReference>()
                     .WithEntityAccess())
            {
                // 스폰 위치 계산
                float3 spawnPos = transform.ValueRO.Position;
                quaternion spawnRot = quaternion.identity;

                // 보정
                if (SystemAPI.HasComponent<HoldableComponent>(entity))
                {
                    var holdable = SystemAPI.GetComponent<HoldableComponent>(entity);
                    if (holdable.HolderEntity != Entity.Null &&
                        SystemAPI.Exists(holdable.HolderEntity) &&
                        SystemAPI.HasComponent<PlayerTag>(holdable.HolderEntity))
                    {
                        var playerTransform = SystemAPI.GetComponent<LocalTransform>(holdable.HolderEntity);
                        spawnPos = playerTransform.Position + new float3(0, 0.6f, 0.5f);
                        spawnRot = playerTransform.Rotation;
                    }
                }

                // 프리팹 생성 요청
                GameObject visual = ItemVisualManager.Instance.SpawnItem(
                    item.ValueRO.IngredientType,
                    spawnPos,
                    spawnRot
                );

                if (visual != null)
                {
                    UpdateVisualColor(visual, item.ValueRO.IngredientType, item.ValueRO.State);

                    ecb.AddComponent(entity, new ItemVisualReference
                    {
                        VisualObject = visual,
                        Type = item.ValueRO.IngredientType,
                        LastState = item.ValueRO.State
                    });
                }
            }

            // 동기화
            foreach (var (transform, visualRef, item, entity) in
                     SystemAPI.Query<RefRO<LocalTransform>, ItemVisualReference, RefRO<ItemComponent>>()
                     .WithEntityAccess())
            {
                if (visualRef.VisualObject != null)
                {
                    Transform visTransform = visualRef.VisualObject.transform;
                    float3 targetPos = transform.ValueRO.Position;

                    bool isHeldByPlayer = false;
                    quaternion holdRotation = quaternion.identity;

                    if (SystemAPI.HasComponent<HoldableComponent>(entity))
                    {
                        var holdable = SystemAPI.GetComponent<HoldableComponent>(entity);
                        if (holdable.HolderEntity != Entity.Null &&
                            SystemAPI.Exists(holdable.HolderEntity) &&
                            SystemAPI.HasComponent<PlayerTag>(holdable.HolderEntity))
                        {
                            var playerTransform = SystemAPI.GetComponent<LocalTransform>(holdable.HolderEntity);
                            targetPos = playerTransform.Position + new float3(0, 0.6f, 0.5f);
                            holdRotation = playerTransform.Rotation;
                            isHeldByPlayer = true;
                        }
                    }

                    visTransform.position = targetPos;

                    if (isHeldByPlayer)
                    {
                        visTransform.rotation = holdRotation;
                    }

                    if (visualRef.LastState != item.ValueRO.State)
                    {
                        UpdateVisualColor(visualRef.VisualObject, item.ValueRO.IngredientType, item.ValueRO.State);
                        visualRef.LastState = item.ValueRO.State;
                    }
                }
            }

            // 반납
            foreach (var (visualRef, entity) in
                     SystemAPI.Query<ItemVisualReference>()
                     .WithNone<ItemComponent>()
                     .WithEntityAccess())
            {
                if (visualRef.VisualObject != null)
                {
                    UpdateVisualColor(visualRef.VisualObject, visualRef.Type, ItemState.Raw);
                    ItemVisualManager.Instance.ReturnItem(visualRef.Type, visualRef.VisualObject);
                }

                ecb.RemoveComponent<ItemVisualReference>(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }


        // 고기색상변경
        private void UpdateVisualColor(GameObject visual, IngredientType type, ItemState state)
        {
            if (type == IngredientType.Burger ||
                type == IngredientType.Wrapper ||
                type == IngredientType.WrappedBurger)
                return;

            var renderer = visual.GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            renderer.GetPropertyBlock(_propBlock);

            Color targetColor = Color.white;
            bool applyColor = true;

            switch (state)
            {
                case ItemState.Raw:
                    if (type == IngredientType.Bread)
                    {
                        applyColor = false;
                    }
                    else if (type == IngredientType.Meat)
                    {
                        targetColor = new Color(1.0f, 0.6f, 0.6f);
                    }
                    else
                    {
                        targetColor = Color.white;
                    }
                    break;

                case ItemState.Cooked:
                    if (type == IngredientType.Meat)
                    {
                        targetColor = new Color(0.6f, 0.3f, 0.1f);
                    }
                    else
                    {
                        applyColor = false;
                    }
                    break;

                case ItemState.Burnt:
                    targetColor = new Color(0.2f, 0.2f, 0.2f);
                    break;
            }

            if (applyColor)
            {
                _propBlock.SetColor("_BaseColor", targetColor);
                renderer.SetPropertyBlock(_propBlock);
            }
            else
            {
                _propBlock.SetColor("_BaseColor", Color.white);
                renderer.SetPropertyBlock(_propBlock);
            }
        }
    }
}