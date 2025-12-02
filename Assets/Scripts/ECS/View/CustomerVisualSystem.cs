using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Meow.ECS.Components;
using Meow.Managers;
using Meow.ECS.View; // Helper 네임스페이스

namespace Meow.ECS.View
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class CustomerVisualSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (CustomerVisualManager.Instance == null) return;

            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            // ==================================================================
            // 1. 생성 (Spawn)
            // ==================================================================
            foreach (var (customer, transform, entity) in
                     SystemAPI.Query<RefRO<CustomerComponent>, RefRO<LocalTransform>>()
                     .WithNone<CustomerVisualReference>()
                     .WithEntityAccess())
            {
                GameObject visual = CustomerVisualManager.Instance.SpawnCustomer(
                    transform.ValueRO.Position, transform.ValueRO.Rotation
                );

                if (visual != null)
                {
                    var helper = visual.GetComponent<CustomerVisualHelper>();

                    if (helper == null)
                    {
                        Debug.LogError($"?? [VisualSystem] '{visual.name}'에 Helper가 없습니다!");
                    }
                    else
                    {
                        // 초기화: 주문 아이콘 & 초기 상태
                        helper.SetupOrderUI(customer.ValueRO.OrderDish);
                        helper.UpdateVisuals(customer.ValueRO.State, true);
                    }

                    // 컴포넌트 부착
                    ecb.AddComponent(entity, new CustomerVisualReference
                    {
                        VisualObject = visual,
                        Helper = helper,
                        LastState = customer.ValueRO.State
                    });
                }
            }

            // ==================================================================
            // 2. 동기화 (Sync)
            // ==================================================================
            foreach (var (transform, visualRef, customer) in
                     SystemAPI.Query<RefRO<LocalTransform>, CustomerVisualReference, RefRO<CustomerComponent>>())
            {
                if (visualRef.VisualObject != null)
                {
                    // A. 위치 동기화
                    visualRef.VisualObject.transform.position = transform.ValueRO.Position;
                    visualRef.VisualObject.transform.rotation = transform.ValueRO.Rotation;

                    if (visualRef.Helper != null)
                    {
                        // B. 상태 변화 감지 (표정/애니메이션)
                        bool isStateChanged = (visualRef.LastState != customer.ValueRO.State);
                        visualRef.Helper.UpdateVisuals(customer.ValueRO.State, isStateChanged);

                        // C. UI 갱신 (매 프레임 - 게이지)
                        visualRef.Helper.UpdateUI(
                            customer.ValueRO.Patience,
                            customer.ValueRO.MaxPatience,
                            customer.ValueRO.State
                        );

                        // 상태 갱신
                        if (isStateChanged) visualRef.LastState = customer.ValueRO.State;
                    }
                }
            }

            // ==================================================================
            // 3. 반납 (Despawn)
            // ==================================================================
            foreach (var (visualRef, entity) in
                     SystemAPI.Query<CustomerVisualReference>()
                     .WithNone<CustomerComponent>()
                     .WithEntityAccess())
            {
                if (visualRef.VisualObject != null)
                {
                    CustomerVisualManager.Instance.ReturnCustomer(visualRef.VisualObject);
                }
                ecb.RemoveComponent<CustomerVisualReference>(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}