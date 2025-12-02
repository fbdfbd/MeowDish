using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 임시 버프 타입
    /// </summary>
    public enum BuffType
    {
        None = 0,
        SpeedUp = 1,
        FastCooking = 2,
        RichCustomer = 3,
        InfinitePatience = 4
    }

    /// <summary>
    /// 시간 제한 임시 버프
    /// 
    /// 사용 시점: Day 2 (24시간 후)
    /// </summary>
    public struct TemporaryBuff : IBufferElementData
    {
        public BuffType Type;
        public float Multiplier;      // 배율 (1.2 = 20% 증가)
        public float Duration;        // 지속 시간
        public float RemainingTime;   // 남은 시간
    }
}