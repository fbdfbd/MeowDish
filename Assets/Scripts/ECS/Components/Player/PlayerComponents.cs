using Unity.Entities;
using Unity.Mathematics;

namespace Meow.ECS.Components
{
    public struct PlayerTag : IComponentData { }

    public struct UnitConfig : IComponentData
    {
        public BlobAssetReference<PlayerBaseStats> BlobRef;
    }

    /// <summary>
    /// 플레이어 불변 스텟
    /// </summary>
    public struct PlayerBaseStats
    {
        public float BaseMoveSpeed;
        public float BaseActionSpeed;
        public float RotationSpeed;
    }


    /// <summary>
    /// 플레이어의 모든 스탯
    /// 
    /// 계산 공식:
    /// 최종값 = (Base + Bonus) * Multiplier * AllMultiplier
    /// </summary>
    public struct PlayerStatsComponent : IComponentData
    {
        // 영구 보너스 (장비, 기타 등등...)
        /// <summary>이동속도 보너스 (장비)</summary>
        public float MoveSpeedBonus;
        /// <summary>작업속도 보너스 (장비)</summary>
        public float ActionSpeedBonus;



        // 임시 배율 (스테이지 내부 버프)
        /// <summary>이동속도 배율 (임시 버프)</summary>
        public float MoveSpeedMultiplier;
        /// <summary>작업속도 배율 (임시 버프)</summary>
        public float ActionSpeedMultiplier;
        /// <summary>모든 속도 배율 (임시 버프)</summary>
        public float AllSpeedMultiplier;
    }

    public struct PlayerAnimationComponent : IComponentData
    {
        public bool IsMoving;
    }

    public struct PlayerInputComponent : IComponentData
    {
        /// <summary>
        /// 조이스틱 오른쪽 : MoveInput = (1.0, 0.0)
        /// </summary>
        public float2 MoveInput;

        /// <summary>
        /// 상호작용 탭 (짧게 한번 누르기)
        /// </summary>
        public bool InteractTapped;

        /// <summary>
        /// 상호작용 홀드 시작 (0.5초 이상 눌렀을 때)
        /// </summary>
        public bool InteractHoldStarted;

        /// <summary>
        /// 상호작용 홀드 중 (계속 누르고 있는 중)
        /// </summary>
        public bool InteractHolding;
    }

    public struct PlayerStateComponent : IComponentData
    {
        /// <summary>
        /// 플레이어 ID (멀티플레이용, 싱글은 0)
        /// </summary>
        public int PlayerId;

        /// <summary>
        /// 현재 들고 있는 아이템
        /// Entity.Null = 빈손
        /// </summary>
        public Entity HeldItemEntity;

        /// <summary>
        /// 아이템을 들고 있는가?
        /// </summary>
        public bool IsHoldingItem;


        /// <summary>
        /// 현재 보고 있는/가까운 스테이션
        /// Entity.Null = 근처에 스테이션 없음
        /// </summary>
        public Entity CurrentStationEntity;

        /// <summary>
        /// 스테이션 근처인가?
        /// </summary>
        public bool IsNearStation;

        /// <summary>
        /// 레이 판정 용 
        /// </summary>
        public float3 LastMoveDirection;  // 마지막 이동 방향
    }
}
