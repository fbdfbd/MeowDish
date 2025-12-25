using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 모든 스테이션 공통 Component
    /// </summary>
    public struct StationComponent : IComponentData
    {
        /// <summary>
        /// 스테이션 타입
        /// </summary>
        public StationType Type;

        /// <summary>
        /// 스테이션 고유 ID
        /// </summary>
        public int StationID;

        /// <summary>
        /// 현재 놓인 아이템
        /// Entity.Null = 아이템 없음
        /// </summary>
        public Entity PlacedItemEntity;

        public bool HasItem => PlacedItemEntity != Entity.Null;
    }

}