using Unity.Entities;
using Unity.Mathematics;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 카운터(작업대) 설정
    /// </summary>
    public struct CounterComponent : IComponentData
    {
        public int MaxItems;
    }

    /// <summary>
    /// 카운터 위에 올려진 아이템들
    /// </summary>
    [InternalBufferCapacity(4)] 
    public struct CounterItemSlot : IBufferElementData
    {
        public Entity ItemEntity;
    }

    /// <summary>
    /// 아이템이 놓일 위치 (Local Position)
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct CounterSnapPoint : IBufferElementData
    {
        public float3 LocalPosition;
    }
}