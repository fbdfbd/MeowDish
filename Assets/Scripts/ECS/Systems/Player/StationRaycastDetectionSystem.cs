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
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]  // ? 가장 마지막에 실행!
    public partial struct StationRaycastDetectionSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            Debug.Log($"========== StationRaycastDetectionSystem 실행됨! ==========");

            // PhysicsWorld가 준비될 때까지 대기
            if (!SystemAPI.TryGetSingleton<PhysicsWorldSingleton>(out var physicsWorldSingleton))
            {
                Debug.LogWarning("[StationRaycast] PhysicsWorld 아직 준비 안 됨!");
                return;
            }

            var physicsWorld = physicsWorldSingleton.PhysicsWorld;

            Debug.Log($"[PhysicsWorld] Body Count: {physicsWorld.NumBodies}");

            // ? Player 개수 확인
            int playerCount = 0;
            foreach (var _ in SystemAPI.Query<RefRO<PlayerStateComponent>>())
            {
                playerCount++;
            }
            Debug.Log($"[Player Count] {playerCount}개 발견!");

            if (playerCount == 0)
            {
                Debug.LogError("? Player가 없습니다! PlayerAuthoring이 실행되었나요?");
                return;
            }

            // ? Query 시작
            Debug.Log($"[Query] Player Query 시작...");

            foreach (var (playerState, playerTransform, playerEntity) in
                     SystemAPI.Query<RefRW<PlayerStateComponent>, RefRO<LocalTransform>>()
                     .WithEntityAccess())
            {
                Debug.Log($"[Query] Player Entity 찾음: {playerEntity}");

                float3 rayDirection = playerState.ValueRO.LastMoveDirection;

                Debug.Log($"[Ray] LastMoveDirection: {rayDirection}");

                if (math.lengthsq(rayDirection) < 0.01f)
                {
                    rayDirection = new float3(0, 0, 1);
                    Debug.Log($"[Ray] Direction이 0이라서 기본값으로 변경: {rayDirection}");
                }
                else
                {
                    rayDirection = math.normalize(rayDirection);
                }

                float interactionRange = 2.0f;

                float3 rayStart = playerTransform.ValueRO.Position;
                rayStart.y = 0.5f;

                float3 rayEnd = rayStart + rayDirection * interactionRange;

                Debug.Log($"[Ray] Start: {rayStart}, End: {rayEnd}, Direction: {rayDirection}");

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

                Debug.Log($"[Ray] Raycast 실행...");

                if (physicsWorld.CastRay(raycastInput, out RaycastHit hit))
                {
                    Debug.Log($"? [HIT!] Entity: {hit.Entity}, Position: {hit.Position}");
                    Debug.DrawLine(rayStart, hit.Position, Color.green, 0.5f);

                    playerState.ValueRW.CurrentStationEntity = hit.Entity;
                    playerState.ValueRW.IsNearStation = true;
                }
                else
                {
                    Debug.Log($"? [MISS] Start: {rayStart}, End: {rayEnd}");
                    Debug.DrawLine(rayStart, rayEnd, Color.red, 0.5f);

                    playerState.ValueRW.CurrentStationEntity = Entity.Null;
                    playerState.ValueRW.IsNearStation = false;
                }
            }

            Debug.Log($"[Query] Player Query 끝!");
            Debug.Log($"========================================");
        }

    }
}
