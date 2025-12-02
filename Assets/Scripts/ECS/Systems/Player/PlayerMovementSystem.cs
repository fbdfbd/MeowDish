using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Meow.ECS.Components;
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
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var physicsWorld = physicsWorldSingleton.PhysicsWorld;

            // 1. 쿼리에 RefRO<UnitConfig> 추가 (Blob 접근용)
            foreach (var (transform,
                          physicsVelocity,
                          input,
                          stats,
                          config,  // <- 여기 추가!
                          playerState,
                          collider,
                          entity) in
                     SystemAPI.Query<
                         RefRW<LocalTransform>,
                         RefRW<PhysicsVelocity>,
                         RefRO<PlayerInputComponent>,
                         RefRO<PlayerStatsComponent>,
                         RefRO<UnitConfig>, // <- 여기 추가!
                         RefRW<PlayerStateComponent>,
                         RefRO<Unity.Physics.PhysicsCollider>>()
                         .WithEntityAccess())
            {
                float2 moveInput = input.ValueRO.MoveInput;

                // 입력이 있을 때만 이동 처리
                if (math.lengthsq(moveInput) > 0.001f)
                {
                    // 2. Blob 데이터 가져오기 (참조)
                    // ref를 써서 복사 비용을 줄입니다.
                    ref var baseStats = ref config.ValueRO.BlobRef.Value;

                    float3 moveDir = new float3(moveInput.x, 0, moveInput.y);
                    moveDir = math.normalize(moveDir);

                    // 3. 최종 속도 직접 계산
                    // 공식: (Base + Bonus) * MoveMult * AllMult
                    float baseSpeed = baseStats.BaseMoveSpeed;
                    float bonusSpeed = stats.ValueRO.MoveSpeedBonus;
                    float moveMult = stats.ValueRO.MoveSpeedMultiplier;
                    float allMult = stats.ValueRO.AllSpeedMultiplier;

                    float finalSpeed = (baseSpeed + bonusSpeed) * moveMult * allMult;

                    // 이동 방향 저장
                    playerState.ValueRW.LastMoveDirection = moveDir;

                    // 이번 프레임 이동량
                    float3 desiredMove = moveDir * finalSpeed * deltaTime;

                    // ----- 충돌 감지 및 처리 (RayCast / Sweep) -----
                    unsafe
                    {
                        var castInput = new ColliderCastInput
                        {
                            Collider = collider.ValueRO.ColliderPtr,
                            Orientation = transform.ValueRO.Rotation,
                            Start = transform.ValueRO.Position,
                            End = transform.ValueRO.Position + desiredMove
                        };

                        if (physicsWorld.CastCollider(castInput, out var hit))
                        {
                            float3 hitNormal = hit.SurfaceNormal;
                            bool movingIntoWall = math.dot(moveDir, hitNormal) < 0f;

                            if (movingIntoWall)
                            {
                                float allowedFraction = hit.Fraction - 0.01f;
                                allowedFraction = math.clamp(allowedFraction, 0f, 1f);

                                float3 safeMove = desiredMove * allowedFraction;
                                transform.ValueRW.Position += safeMove;
                                physicsVelocity.ValueRW.Linear = float3.zero;
                            }
                            else
                            {
                                transform.ValueRW.Position += desiredMove;
                                physicsVelocity.ValueRW.Linear = desiredMove / deltaTime;
                            }
                        }
                        else
                        {
                            transform.ValueRW.Position += desiredMove;
                            physicsVelocity.ValueRW.Linear = desiredMove / deltaTime;
                        }
                    }

                    physicsVelocity.ValueRW.Angular = float3.zero;

                    // 4. 회전 처리 (Blob에서 회전 속도 가져오기)
                    quaternion targetRot = quaternion.LookRotation(moveDir, math.up());
                    transform.ValueRW.Rotation = math.slerp(
                        transform.ValueRO.Rotation,
                        targetRot,
                        baseStats.RotationSpeed * deltaTime // <- 여기서 Blob 값 사용
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