using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Meow.ECS.Components;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// Raycast 기반 스테이션 감지 시스템
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct StationRaycastDetectionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<GamePauseComponent>(out var pause) && pause.IsPaused)
                return;

            if (!SystemAPI.TryGetSingleton<PhysicsWorldSingleton>(out var physicsWorldSingleton))
                return;

            var physicsWorld = physicsWorldSingleton.PhysicsWorld;

            foreach (var (playerState, playerTransform, playerEntity) in
                     SystemAPI.Query<RefRW<PlayerStateComponent>, RefRO<LocalTransform>>()
                              .WithEntityAccess())
            {
                float3 rayDirection = playerState.ValueRO.LastMoveDirection;

                if (math.lengthsq(rayDirection) < 0.01f)
                {
                    rayDirection = new float3(0f, 0f, 1f);
                }
                else
                {
                    rayDirection = math.normalize(rayDirection);
                }

                const float interactionRange = 0.5f;

                float3 rayStart = playerTransform.ValueRO.Position;
                rayStart.y = 0.1f;

                float3 rayEnd = rayStart + rayDirection * interactionRange;

                var raycastInput = new RaycastInput
                {
                    Start = rayStart,
                    End = rayEnd,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = 1u << 0,
                        CollidesWith = 1u << 6,
                        GroupIndex = 0
                    }
                };

                if (physicsWorld.CastRay(raycastInput, out RaycastHit hit))
                {
                    playerState.ValueRW.CurrentStationEntity = hit.Entity;
                    playerState.ValueRW.IsNearStation = true;
                }
                else
                {
                    playerState.ValueRW.CurrentStationEntity = Entity.Null;
                    playerState.ValueRW.IsNearStation = false;
                }
            }
        }
    }
}
