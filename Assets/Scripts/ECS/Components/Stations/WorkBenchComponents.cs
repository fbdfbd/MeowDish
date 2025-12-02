using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 빈 작업대
    /// 
    /// 여러 아이템을 놓을 수 있음
    /// 아이템 위에서 작업 가능 (자르기 등)
    /// </summary>
    public struct WorkBenchComponent : IComponentData
    {
        /// <summary>
        /// 최대 아이템 개수
        /// </summary>
        public int MaxItems;

        /// <summary>
        /// 현재 아이템 개수
        /// </summary>
        public int CurrentItemCount;

        /// <summary>
        /// 가득 찼는가?
        /// </summary>
        public bool IsFull => CurrentItemCount >= MaxItems;

        /// <summary>
        /// 비어있는가?
        /// </summary>
        public bool IsEmpty => CurrentItemCount <= 0;
    }

    /// <summary>
    /// 작업대에 놓인 아이템들 (DynamicBuffer)
    /// </summary>
    [InternalBufferCapacity(10)]
    public struct WorkBenchItem : IBufferElementData
    {
        public Entity ItemEntity;
    }
}