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
    // UpdateAfter 제거! (다른 그룹이라 안됨)
    // PlayerMovementSystem이 FixedStep에서 먼저 실행되므로 자동으로 순서 보장됨
    public partial struct StationRaycastDetectionSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

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
                float3 rayEnd = rayStart + rayDirection * interactionRange;

                var raycastInput = new RaycastInput
                {
                    Start = rayStart,
                    End = rayEnd,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = ~0u,
                        CollidesWith = 1u << 6,
                        GroupIndex = 0
                    }
                };

                if (physicsWorld.CastRay(raycastInput, out RaycastHit hit))
                {
                    Entity hitEntity = hit.Entity;

                    if (SystemAPI.HasComponent<StationComponent>(hitEntity) &&
                        SystemAPI.HasComponent<InteractableComponent>(hitEntity))
                    {
                        var interactable = SystemAPI.GetComponent<InteractableComponent>(hitEntity);

                        if (interactable.IsActive)
                        {
                            playerState.ValueRW.CurrentStationEntity = hitEntity;
                            playerState.ValueRW.IsNearStation = true;

                            var stationType = SystemAPI.GetComponent<StationComponent>(hitEntity).Type;
                            Debug.Log($"[Raycast 감지: {stationType}]");
                        }
                    }
                    else
                    {
                        ClearStation(ref playerState.ValueRW);
                    }
                }
                else
                {
                    ClearStation(ref playerState.ValueRW);
                }
            }
        }

        private void ClearStation(ref PlayerStateComponent playerState)
        {
            playerState.CurrentStationEntity = Entity.Null;
            playerState.IsNearStation = false;
        }
    }
}