using Unity.Entities;
using Unity.Mathematics;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 플레이어 상태
    /// 
    /// 아이템 소지, 스테이션 상호작용 정보
    /// </summary>
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

        // 플레이어 방향 (Raycast용)
        public float3 LastMoveDirection;  // 마지막 이동 방향
    }
}