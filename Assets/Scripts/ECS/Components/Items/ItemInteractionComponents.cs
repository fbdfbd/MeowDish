using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 들 수 있는 아이템 표시
    /// </summary>
    public struct HoldableComponent : IComponentData
    {
        /// <summary>
        /// 누가 들고 있는가?
        /// Entity.Null = 아무도 안 듦
        /// </summary>
        public Entity HolderEntity;

        public bool IsHeld => HolderEntity != Entity.Null;
    }
}