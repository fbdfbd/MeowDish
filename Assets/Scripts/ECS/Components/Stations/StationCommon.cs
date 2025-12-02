using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 모든 스테이션의 공통 Component
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
        /// 현재 놓인 아이템 (1개만 - WorkBench 제외)
        /// Entity.Null = 아이템 없음
        /// </summary>
        public Entity PlacedItemEntity;

        /// <summary>
        /// 아이템이 있는가?
        /// </summary>
        public bool HasItem => PlacedItemEntity != Entity.Null;
    }

    // --- 스테이션 타입 태그들 ---

    public struct ContainerStationTag : IComponentData { }  // 재료 컨테이너
    public struct CounterStationTag : IComponentData { }    // 카운터 / 작업대
    public struct StoveStationTag : IComponentData { }      // 스토브 (굽기)
    public struct TrashCanTag : IComponentData { }

    // 나중에 필요하면 AssemblyStationTag, SinkStationTag 추가
}