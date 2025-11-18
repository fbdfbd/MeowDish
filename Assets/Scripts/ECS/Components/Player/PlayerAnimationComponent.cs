using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 플레이어 실시간 애니메이션 상태
    /// 
    /// Idle ↔ Walk만 사용
    /// </summary>
    public struct PlayerAnimationComponent : IComponentData
    {
        public bool IsMoving;
    }
}