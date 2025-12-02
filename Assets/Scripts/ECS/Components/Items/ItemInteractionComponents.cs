using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 들 수 있는 아이템 표시
    /// 
    /// 이 Component가 있어야 플레이어가 집어 들 수 있음 (Pickup System 대상)
    /// </summary>
    public struct HoldableComponent : IComponentData
    {
        /// <summary>
        /// 누가 들고 있는가?
        /// Entity.Null = 아무도 안 듦 (바닥이나 작업대에 있음)
        /// </summary>
        public Entity HolderEntity;

        /// <summary>
        /// 들려있는 상태인가?
        /// </summary>
        public bool IsHeld => HolderEntity != Entity.Null;
    }
}