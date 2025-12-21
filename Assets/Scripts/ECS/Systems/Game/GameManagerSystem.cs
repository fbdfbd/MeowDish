using Unity.Burst;
using Unity.Entities;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct GameManagerSystem : ISystem
    {
        [BurstCompile] public void OnCreate(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonRW<GameSessionComponent>(out var sessionRW))
                return;

            ref var session = ref sessionRW.ValueRW;

            // 스테이지 초기화(버프 적용) 한 번만
            if (!session.IsStageInitialized && session.State == GameState.Playing)
            {
                ApplyBuffs(ref state);
                session.IsStageInitialized = true;
            }

            if (session.State == GameState.Playing)
            {
                if (session.CurrentFailures >= session.MaxFailures)
                {
                    session.State = GameState.GameOver;
                }

                if (session.ProcessedCount >= session.TotalCustomers &&
                    session.State != GameState.GameOver)
                {
                    session.State = GameState.StageClear;
                }
            }
        }

        private void ApplyBuffs(ref SystemState state)
        {
            var buffBuffer = SystemAPI.GetSingletonBuffer<ActiveBuffElement>();

            bool hasSpeedBuff = false;
            bool hasCookBuff = false;

            foreach (var buff in buffBuffer)
            {
                if (buff.Type == BuffType.SpeedUp) hasSpeedBuff = true;
                if (buff.Type == BuffType.FastCooking) hasCookBuff = true;
            }

            foreach (var stats in SystemAPI.Query<RefRW<PlayerStatsComponent>>())
            {
                stats.ValueRW.MoveSpeedMultiplier = hasSpeedBuff ? 1.5f : 1.0f;
            }

            foreach (var stove in SystemAPI.Query<RefRW<StoveComponent>>())
            {
                stove.ValueRW.CookingSpeedMultiplier = hasCookBuff ? 2.0f : 1.0f;
            }
        }
    }
}
