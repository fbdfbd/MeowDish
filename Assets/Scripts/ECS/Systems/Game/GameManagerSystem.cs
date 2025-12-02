using Unity.Entities;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class GameManagerSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!SystemAPI.TryGetSingletonRW<GameSessionComponent>(out var sessionRW))
                return;

            ref var session = ref sessionRW.ValueRW;

            // ==========================================================
            // 1. [최적화] 스테이지 초기화 (버프 적용) - 딱 한 번만 실행!
            // ==========================================================
            if (!session.IsStageInitialized && session.State == GameState.Playing)
            {
                ApplyBuffs();
                session.IsStageInitialized = true; // 체크 완료! 이제 이 if문은 다시 안 들어옴
                Debug.Log("[GameManager] 스테이지 시작! 버프 적용 완료.");
            }

            // ==========================================================
            // 2. 승패 판정 (매 프레임 체크하지만 연산 매우 가벼움)
            // ==========================================================
            if (session.State == GameState.Playing)
            {
                // A. 게임 오버 체크 (3아웃)
                if (session.CurrentFailures >= session.MaxFailures)
                {
                    session.State = GameState.GameOver;
                    Debug.Log($"[게임 오버] 3번 실패했습니다! ㅠㅠ (점수: {session.CurrentScore})");
                }

                // B. 스테이지 클리어 체크 (모든 손님 처리 완료)
                // (성공했든 실패했든 일단 손님이 다 지나갔으면 끝)
                if (session.ProcessedCount >= session.TotalCustomers)
                {
                    // 아직 게임오버가 아니라면 클리어!
                    if (session.State != GameState.GameOver)
                    {
                        session.State = GameState.StageClear;

                        // 별점 계산 예시
                        int stars = 3;
                        if (session.CurrentFailures == 1) stars = 2;
                        else if (session.CurrentFailures == 2) stars = 1;

                        Debug.Log($"[영업 종료] 스테이지 클리어! 별점: {stars}개 ? (점수: {session.CurrentScore})");
                    }
                }
            }
        }

        // ?? 버프 적용 함수 (따로 뺌)
        private void ApplyBuffs()
        {
            var buffBuffer = SystemAPI.GetSingletonBuffer<ActiveBuffElement>();

            bool hasSpeedBuff = false;
            bool hasCookBuff = false;

            foreach (var buff in buffBuffer)
            {
                if (buff.Type == BuffType.SpeedUp) hasSpeedBuff = true;
                if (buff.Type == BuffType.FastCooking) hasCookBuff = true;
            }

            // 플레이어 이속 적용 (EntityQuery로 한 번만 싹 훑음)
            foreach (var stats in SystemAPI.Query<RefRW<PlayerStatsComponent>>())
            {
                stats.ValueRW.MoveSpeedMultiplier = hasSpeedBuff ? 1.5f : 1.0f;
            }

            // 스토브 속도 적용
            foreach (var stove in SystemAPI.Query<RefRW<StoveComponent>>())
            {
                stove.ValueRW.CookingSpeedMultiplier = hasCookBuff ? 2.0f : 1.0f;
            }
        }
    }
}