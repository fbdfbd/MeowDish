using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 작업대
    /// 아이템 보관용.
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
 
        public bool IsFull => CurrentItemCount >= MaxItems;

        public bool IsEmpty => CurrentItemCount <= 0;
    }

    /// <summary>
    /// 작업대에 놓인 아이템들(DynamicBuffer)
    /// </summary>
    [InternalBufferCapacity(10)]
    public struct WorkBenchItem : IBufferElementData
    {
        public Entity ItemEntity;
    }
}