using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMovementSystem))]
    public partial struct PlayerAnimationSystem : ISystem
    {
        private const float MoveThreshold = 0.1f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerAnimationComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (animation, input) in
                SystemAPI.Query<
                    RefRW<PlayerAnimationComponent>,
                    RefRO<PlayerInputComponent>>())
            {
                float inputMag = math.length(input.ValueRO.MoveInput);
                animation.ValueRW.IsMoving = inputMag > 0.1f;
            }
        }
    }
}