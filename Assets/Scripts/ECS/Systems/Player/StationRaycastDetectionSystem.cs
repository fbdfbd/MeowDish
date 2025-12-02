using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Meow.ECS.Components;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// Raycast 기반 스테이션 감지 시스템
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct StationRaycastDetectionSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<PhysicsWorldSingleton>(out var physicsWorldSingleton))
            {
                Debug.LogWarning("[StationRaycast] PhysicsWorld 아직 준비 안 됨!");
                return;
            }

            var physicsWorld = physicsWorldSingleton.PhysicsWorld;


            foreach (var (playerState, playerTransform, playerEntity) in
                     SystemAPI.Query<RefRW<PlayerStateComponent>, RefRO<LocalTransform>>()
                     .WithEntityAccess())
            {
                float3 rayDirection = playerState.ValueRO.LastMoveDirection;

                if (math.lengthsq(rayDirection) < 0.01f)
                {
                    rayDirection = new float3(0, 0, 1);
                }
                else
                {
                    rayDirection = math.normalize(rayDirection);
                }

                float interactionRange = 2.0f;

                float3 rayStart = playerTransform.ValueRO.Position;
                rayStart.y = 0.5f;

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

                Debug.DrawLine(rayStart, rayEnd, Color.yellow, 0.5f);

                if (physicsWorld.CastRay(raycastInput, out RaycastHit hit))
                {
                    Debug.DrawLine(rayStart, hit.Position, Color.green, 0.5f);

                    playerState.ValueRW.CurrentStationEntity = hit.Entity;
                    playerState.ValueRW.IsNearStation = true;
                }
                else
                {
                    Debug.DrawLine(rayStart, rayEnd, Color.red, 0.5f);

                    playerState.ValueRW.CurrentStationEntity = Entity.Null;
                    playerState.ValueRW.IsNearStation = false;
                }

            }
        }

    }
}



/*
교체 : Burst추후
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst; // Burst 네임스페이스 추가
using Meow.ECS.Components;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// Raycast 기반 스테이션 감지 시스템
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [BurstCompile] // 시스템 전체에 Burst 적용
    public partial struct StationRaycastDetectionSystem : ISystem
    {
        [BurstCompile] // OnUpdate에 Burst 적용
        public void OnUpdate(ref SystemState state)
        {
            // PhysicsWorldSingleton이 없으면 아무것도 하지 않음
            if (!SystemAPI.TryGetSingleton<PhysicsWorldSingleton>(out var physicsWorldSingleton))
            {
                return;
            }

            var physicsWorld = physicsWorldSingleton.PhysicsWorld;

            // Player 쿼리 및 Raycast 처리
            foreach (var (playerState, playerTransform) in
                     SystemAPI.Query<RefRW<PlayerStateComponent>, RefRO<LocalTransform>>())
            {
                float3 rayDirection = playerState.ValueRO.LastMoveDirection;

                // 방향 벡터 유효성 검사 (길이가 너무 짧으면 기본값 Z축)
                if (math.lengthsq(rayDirection) < 0.01f)
                {
                    rayDirection = new float3(0, 0, 1);
                }
                else
                {
                    rayDirection = math.normalize(rayDirection);
                }

                float interactionRange = 2.0f;
                float3 rayStart = playerTransform.ValueRO.Position;
                rayStart.y = 0.5f; // 높이 보정

                float3 rayEnd = rayStart + (rayDirection * interactionRange);

                var raycastInput = new RaycastInput
                {
                    Start = rayStart,
                    End = rayEnd,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = 1u << 0,   // Player 레이어 (예시)
                        CollidesWith = 1u << 6, // Station 레이어 (예시)
                        GroupIndex = 0
                    }
                };

                // Raycast 수행
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







*/

