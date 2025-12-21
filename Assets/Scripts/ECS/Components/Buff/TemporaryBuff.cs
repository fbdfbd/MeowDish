using Unity.Entities;

namespace Meow.ECS.Components
{
    public enum BuffType
    {
        None = 0,
        SpeedUp = 1,
        FastCooking = 2,
        RichCustomer = 3,
        InfinitePatience = 4,
        ExtraLife = 5,
        TipBoost = 6,
        SlowSpawn = 7
    }


    public struct TemporaryBuff : IBufferElementData
    {
        public BuffType Type;
        public float Multiplier;      // 배율 (1.2 = 20% 증가)
        public float Duration;        // 지속 시간
        public float RemainingTime;   // 남은 시간
    }
}