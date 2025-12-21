using Unity.Entities;

namespace Meow.ECS.Components
{
    // 게임 진행 상태
    public enum GameState
    {
        Ready,      // 시작 대기
        Playing,    // 영업 중
        StageClear, // 영업 종료 (성공)
        GameOver    // 폐업 (실패)
    }

    public struct GamePauseComponent : IComponentData
    {
        public bool IsPaused;
    }

    // 전역 게임 데이터
    public struct GameSessionComponent : IComponentData
    {
        public GameState State;

        // 스테이지 진행 정보
        public int MaxFailures;      // 허용된 최대 실패 수(생명)
        public int CurrentFailures;  // 현재 실패 수

        public int TotalCustomers;   // 오늘 방문할 총 손님 수
        public int ServedCustomers;  // 성공한 손님 수
        public int ProcessedCount;

        public int CurrentScore; 
        public float ScoreMultiplier;
        public int CurrentStageLevel;

        public bool IsStageInitialized;
    }

    [InternalBufferCapacity(8)]
    public struct ActiveBuffElement : IBufferElementData
    {
        public BuffType Type;
    }
}
