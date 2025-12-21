using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 업그레이드 타입
    /// </summary>
    public enum UpgradeType
    {
        MoveSpeed = 0,       // 이동속도
        ActionSpeed = 1,     // 작업속도
        AllSpeed = 2,        // 모든 속도
    }

    /// <summary>
    /// 획득한 영구 업그레이드
    /// </summary>
    public struct PermanentUpgrade : IBufferElementData
    {
        public UpgradeType Type;
        public float BonusValue;    // 절댓값 또는 배율
        public bool IsMultiplier;   // true = 배율, false = 절댓값
        public int DayAcquired;     // 획득한 날짜
    }
}