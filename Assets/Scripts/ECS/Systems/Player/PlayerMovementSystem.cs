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

            foreach (var (transform, physicsVelocity, input, stats) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRW<PhysicsVelocity>, RefRO<PlayerInputComponent>, RefRO<PlayerStatsComponent>>())
            {
                float2 moveInput = input.ValueRO.MoveInput;

                if (math.lengthsq(moveInput) > 0.001f)
                {
                    float3 moveDir = new float3(moveInput.x, 0, moveInput.y);
                    moveDir = math.normalize(moveDir);
                    float finalSpeed = stats.ValueRO.GetFinalMoveSpeed();

                    // Kinematic: Transform 직접 업데이트
                    transform.ValueRW.Position += moveDir * finalSpeed * deltaTime;

                    // Velocity 설정 (충돌 감지용)
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
                    physicsVelocity.ValueRW.Linear = float3.zero;
                    physicsVelocity.ValueRW.Angular = float3.zero;
                }
            }
        }
    }
}