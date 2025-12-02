using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CustomerSpawnerSystem : SystemBase
    {
        private Unity.Mathematics.Random _random;

        protected override void OnCreate()
        {
            _random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (spawner, transform, menuBuffer) in
                     SystemAPI.Query<RefRW<CustomerSpawnerComponent>, RefRO<LocalTransform>, DynamicBuffer<PossibleMenuElement>>())
            {
                // ?? [추가] 이미 다 왔으면 스폰 중단!
                if (spawner.ValueRO.SpawnedCount >= spawner.ValueRO.MaxCustomersPerStage) continue;

                spawner.ValueRW.Timer += deltaTime;

                if (spawner.ValueRW.Timer >= spawner.ValueRO.SpawnInterval)
                {
                    spawner.ValueRW.Timer = 0f;

                    // 1. 손님 생성
                    Entity customer = ecb.CreateEntity();

                    // 2. 메뉴 랜덤
                    if (menuBuffer.Length > 0)
                    {
                        int menuIndex = _random.NextInt(0, menuBuffer.Length);
                        IngredientType orderedDish = menuBuffer[menuIndex].DishType;

                        // 3. 컴포넌트 설정
                        ecb.AddComponent(customer, new CustomerComponent
                        {
                            CustomerID = _random.NextInt(10000, 99999),
                            State = CustomerState.Spawned,
                            TargetStation = Entity.Null,
                            QueueIndex = -1,
                            OrderDish = orderedDish,
                            Patience = spawner.ValueRO.MaxPatience,
                            MaxPatience = spawner.ValueRO.MaxPatience,
                            WalkSpeed = spawner.ValueRO.WalkSpeed
                        });

                        ecb.AddComponent<CustomerTag>(customer);
                        ecb.AddComponent(customer, LocalTransform.FromPosition(transform.ValueRO.Position));

                        // ?? [추가] 카운트 증가
                        spawner.ValueRW.SpawnedCount++;

                        Debug.Log($"[손님 등장 ({spawner.ValueRO.SpawnedCount}/{spawner.ValueRO.MaxCustomersPerStage})] 메뉴: {orderedDish}");
                    }
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}