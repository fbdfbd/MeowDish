using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Meow.ECS.Components;
using UnityEngine;
using Unity.Physics.Systems;

namespace Meow.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct PlayerMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerInputComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // PlayerStateComponent 추가!
            foreach (var (transform, physicsVelocity, input, stats, playerState) in
                SystemAPI.Query<RefRW<LocalTransform>,
                               RefRW<PhysicsVelocity>,
                               RefRO<PlayerInputComponent>,
                               RefRO<PlayerStatsComponent>,
                               RefRW<PlayerStateComponent>>())  // 추가!
            {
                float2 moveInput = input.ValueRO.MoveInput;

                if (math.lengthsq(moveInput) > 0.001f)
                {
                    float3 moveDir = new float3(moveInput.x, 0, moveInput.y);
                    moveDir = math.normalize(moveDir);
                    float finalSpeed = stats.ValueRO.GetFinalMoveSpeed();

                    // 이동 방향 저장 (Raycast용)
                    playerState.ValueRW.LastMoveDirection = moveDir;

                    // Kinematic: Transform 직접 업데이트
                    transform.ValueRW.Position += moveDir * finalSpeed * deltaTime;

                    // Velocity 설정 (충돌 처리용)
                    physicsVelocity.ValueRW.Linear = moveDir * finalSpeed;
                    physicsVelocity.ValueRW.Angular = float3.zero;

                    // 회전
                    quaternion targetRot = quaternion.LookRotation(moveDir, math.up());
                    transform.ValueRW.Rotation = math.slerp(
                        transform.ValueRO.Rotation,
                        targetRot,
                        stats.ValueRO.RotationSpeed * deltaTime
                    );
                }
                else
                {
                    // 정지 중에는 LastMoveDirection 유지 (마지막 방향 기억)
                    physicsVelocity.ValueRW.Linear = float3.zero;
                    physicsVelocity.ValueRW.Angular = float3.zero;
                }
            }
        }
    }
}