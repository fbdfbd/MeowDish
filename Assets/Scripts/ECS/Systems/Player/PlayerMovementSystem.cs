using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
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

            foreach (var (transform, input, stats) in
                SystemAPI.Query<
                    RefRW<LocalTransform>,
                    RefRO<PlayerInputComponent>,
                    RefRO<PlayerStatsComponent>>())
            {
                float2 moveInput = input.ValueRO.MoveInput;

                if (math.lengthsq(moveInput) < 0.001f)
                    continue;

                float3 moveDir = new float3(moveInput.x, 0, moveInput.y);
                moveDir = math.normalize(moveDir);

                float finalSpeed = stats.ValueRO.GetFinalMoveSpeed();

                transform.ValueRW.Position += moveDir * finalSpeed * deltaTime;

                quaternion targetRot = quaternion.LookRotation(moveDir, math.up());
                transform.ValueRW.Rotation = math.slerp(
                    transform.ValueRO.Rotation,
                    targetRot,
                    stats.ValueRO.RotationSpeed * deltaTime
                );
            }
        }
    }
}