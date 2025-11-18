using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 플레이어의 모든 스탯
    /// 
    /// 계산 공식:
    /// 최종값 = (Base + Bonus) * Multiplier * AllMultiplier
    /// </summary>
    public struct PlayerStatsComponent : IComponentData
    {
        // Layer 1: 기본 스탯 (불변)
        /// <summary>기본 이동속도 (m/s)</summary>
        public float BaseMoveSpeed;
        /// <summary>기본 작업속도 (배율)</summary>
        public float BaseActionSpeed;
        /// <summary>회전 속도 (lerp 배율)</summary>
        public float RotationSpeed;



        // Layer 2: 영구 보너스 (장비, 기타 등등...)
        /// <summary>이동속도 보너스 (장비)</summary>
        public float MoveSpeedBonus;
        /// <summary>작업속도 보너스 (장비)</summary>
        public float ActionSpeedBonus;



        // Layer 3: 임시 배율 (스테이지 내부 버프)
        /// <summary>이동속도 배율 (임시 버프)</summary>
        public float MoveSpeedMultiplier;
        /// <summary>작업속도 배율 (임시 버프)</summary>
        public float ActionSpeedMultiplier;
        /// <summary>모든 속도 배율 (임시 버프)</summary>
        public float AllSpeedMultiplier;



        /// <summary>
        /// 최종 이동속도 계산
        /// </summary>
        public float GetFinalMoveSpeed()
        {
            return (BaseMoveSpeed + MoveSpeedBonus) *
                   MoveSpeedMultiplier *
                   AllSpeedMultiplier;
        }

        /// <summary>
        /// 최종 작업속도 계산
        /// </summary>
        public float GetFinalActionSpeed()
        {
            return (BaseActionSpeed + ActionSpeedBonus) *
                   ActionSpeedMultiplier *
                   AllSpeedMultiplier;
        }
    }
}