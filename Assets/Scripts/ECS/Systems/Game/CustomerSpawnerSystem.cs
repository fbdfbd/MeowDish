using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CustomerSpawnerSystem : ISystem
    {
        private Unity.Mathematics.Random _random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
             _random = Unity.Mathematics.Random.CreateFromIndex(0x1234u);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<GamePauseComponent>(out var pause) && pause.IsPaused) return;

            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            bool canSpawn = false;
            if (SystemAPI.TryGetSingleton<GameSessionComponent>(out var session))
                canSpawn = session.State == GameState.Playing;

            foreach (var (spawner, transform, menuBuffer) in
                     SystemAPI.Query<RefRW<CustomerSpawnerComponent>, RefRO<LocalTransform>, DynamicBuffer<PossibleMenuElement>>())
            {
                if (!spawner.ValueRO.IsActive || !canSpawn) continue;
                if (spawner.ValueRO.SpawnedCount >= spawner.ValueRO.MaxCustomersPerStage) continue;

                spawner.ValueRW.Timer += deltaTime;

                if (spawner.ValueRO.Timer >= spawner.ValueRO.SpawnInterval)
                {
                    spawner.ValueRW.Timer = 0f;

                    if (menuBuffer.Length > 0)
                    {
                        int menuIndex = _random.NextInt(0, menuBuffer.Length);
                        IngredientType orderedDish = menuBuffer[menuIndex].DishType;

                        Entity customer = ecb.CreateEntity();
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

                        spawner.ValueRW.SpawnedCount++;
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
