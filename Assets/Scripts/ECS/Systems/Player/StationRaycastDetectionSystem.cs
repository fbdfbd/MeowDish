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
            // 게임 정지 상태면 레이캐스트 중단
            if (SystemAPI.TryGetSingleton<GamePauseComponent>(out var pause))
            {
                if (pause.IsPaused) return;
            }

            if (!SystemAPI.TryGetSingleton<PhysicsWorldSingleton>(out var physicsWorldSingleton))
            {
                return;
            }

            var physicsWorld = physicsWorldSingleton.PhysicsWorld;

            foreach (var (playerState, playerTransform, playerEntity) in
                     SystemAPI.Query<RefRW<PlayerStateComponent>, RefRO<LocalTransform>>()
                     .WithEntityAccess())
            {
                // 방향 벡터
                float3 rayDirection = playerState.ValueRO.LastMoveDirection;

                if (math.lengthsq(rayDirection) < 0.01f)
                {
                    rayDirection = new float3(0, 0, 1);
                }
                else
                {
                    rayDirection = math.normalize(rayDirection);
                }


                // 거리
                float interactionRange = 0.5f;

                float3 rayStart = playerTransform.ValueRO.Position;

                // 높이
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

                // 노란선: 레이 발사 경로
                Debug.DrawLine(rayStart, rayEnd, Color.yellow, 0.5f);

                if (physicsWorld.CastRay(raycastInput, out RaycastHit hit))
                {
                    // 초록선: 충돌
                    Debug.DrawLine(rayStart, hit.Position, Color.green, 0.5f);

                    playerState.ValueRW.CurrentStationEntity = hit.Entity;
                    playerState.ValueRW.IsNearStation = true;
                }
                else
                {
                    // 빨간선: 허공
                    Debug.DrawLine(rayStart, rayEnd, Color.red, 0.5f);

                    playerState.ValueRW.CurrentStationEntity = Entity.Null;
                    playerState.ValueRW.IsNearStation = false;
                }
            }
        }
    }
}