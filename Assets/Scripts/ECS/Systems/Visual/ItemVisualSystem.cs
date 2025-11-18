using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Meow.ECS.Components;
using System.Collections.Generic;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// 아이템 Entity에 시각적 GameObject 연결
    /// 들고 있는 아이템은 플레이어 따라다님
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class ItemVisualSystem : SystemBase
    {
        // Entity와 GameObject 매핑
        private Dictionary<Entity, GameObject> itemVisuals = new Dictionary<Entity, GameObject>();

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            // ????????????????????????????????????????
            // 1. 새로운 아이템 Entity에 GameObject 생성
            // ????????????????????????????????????????
            Entities
                .WithAll<ItemComponent, HoldableComponent>()
                .WithNone<ItemVisualTag>()  // 아직 시각화 안 된 것만
                .ForEach((Entity itemEntity, in ItemComponent item) =>
                {
                    // GameObject 생성
                    GameObject visual = CreateItemVisual(item);
                    itemVisuals[itemEntity] = visual;

                    // Tag 추가 (중복 생성 방지)
                    ecb.AddComponent<ItemVisualTag>(itemEntity);

                    Debug.Log($"[ItemVisualSystem] Created visual for {item.IngredientType}");

                }).WithoutBurst().Run();

            // ????????????????????????????????????????
            // 2. 아이템 위치 업데이트
            // ????????????????????????????????????????
            Entities
                .WithAll<ItemComponent, HoldableComponent, ItemVisualTag>()
                .ForEach((Entity itemEntity, in LocalTransform itemTransform, in HoldableComponent holdable) =>
                {
                    if (itemVisuals.TryGetValue(itemEntity, out GameObject visual))
                    {
                        if (visual != null)
                        {
                            // 들려있는 경우 - 플레이어 위에 표시
                            if (holdable.IsHeld && SystemAPI.Exists(holdable.HolderEntity))
                            {
                                var holderTransform = SystemAPI.GetComponent<LocalTransform>(holdable.HolderEntity);
                                visual.transform.position = holderTransform.Position + new float3(0, 1.5f, 0.5f);
                                visual.SetActive(true);
                            }
                            // 안 들려있는 경우 - 아이템 자체 위치
                            else
                            {
                                visual.transform.position = itemTransform.Position;
                                visual.SetActive(true);
                            }
                        }
                    }
                }).WithoutBurst().Run();

            // ????????????????????????????????????????
            // 3. 삭제된 Entity의 GameObject 정리
            // ????????????????????????????????????????
            var entitiesToRemove = new List<Entity>();
            foreach (var kvp in itemVisuals)
            {
                if (!SystemAPI.Exists(kvp.Key))
                {
                    if (kvp.Value != null)
                        GameObject.Destroy(kvp.Value);
                    entitiesToRemove.Add(kvp.Key);
                }
            }

            foreach (var entity in entitiesToRemove)
            {
                itemVisuals.Remove(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private GameObject CreateItemVisual(ItemComponent item)
        {
            GameObject visual = null;

            // 재료 타입별 시각적 표현
            switch (item.IngredientType)
            {
                case IngredientType.Bread:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visual.transform.localScale = Vector3.one * 0.3f;
                    visual.GetComponent<Renderer>().material.color = new Color(0.9f, 0.7f, 0.4f);
                    break;

                case IngredientType.Meat:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    visual.transform.localScale = Vector3.one * 0.4f;
                    visual.GetComponent<Renderer>().material.color = new Color(0.8f, 0.2f, 0.2f);
                    break;

                case IngredientType.Lettuce:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    visual.transform.localScale = new Vector3(0.4f, 0.1f, 0.4f);
                    visual.GetComponent<Renderer>().material.color = new Color(0.2f, 0.8f, 0.2f);
                    break;

                case IngredientType.Tomato:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    visual.transform.localScale = Vector3.one * 0.3f;
                    visual.GetComponent<Renderer>().material.color = new Color(1f, 0.3f, 0.3f);
                    break;

                case IngredientType.Cheese:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visual.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);
                    visual.GetComponent<Renderer>().material.color = new Color(1f, 0.9f, 0.3f);
                    break;

                case IngredientType.Plate:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    visual.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
                    visual.GetComponent<Renderer>().material.color = Color.white;
                    break;

                default:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    visual.transform.localScale = Vector3.one * 0.3f;
                    visual.GetComponent<Renderer>().material.color = Color.magenta;
                    break;
            }

            // Collider 제거 (플레이어와 충돌 방지)
            var collider = visual.GetComponent<Collider>();
            if (collider != null)
                GameObject.Destroy(collider);

            visual.name = $"Item_{item.IngredientType}_{item.ItemID}";

            // 상태별 추가 효과
            if (item.State == ItemState.Chopped)
            {
                // 썰린 표시 - 크기 줄이기
                visual.transform.localScale *= 0.8f;
            }
            else if (item.State == ItemState.Cooked)
            {
                // 익힌 표시 - 색 어둡게
                var renderer = visual.GetComponent<Renderer>();
                renderer.material.color *= 0.7f;
            }

            return visual;
        }

        protected override void OnDestroy()
        {
            // 시스템 종료 시 모든 GameObject 정리
            foreach (var kvp in itemVisuals)
            {
                if (kvp.Value != null)
                    GameObject.Destroy(kvp.Value);
            }
            itemVisuals.Clear();
        }
    }

    /// <summary>
    /// 아이템이 시각화되었음을 표시하는 태그
    /// </summary>
    public struct ItemVisualTag : IComponentData { }
}