using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 상호작용 가능한 오브젝트 표시
    /// 
    /// 스테이션에 붙어서 플레이어가 E 키로 상호작용 가능하게 함
    /// </summary>
    public struct InteractableComponent : IComponentData
    {
        /// <summary>
        /// 활성화 여부
        /// </summary>
        public bool IsActive;

        /// <summary>
        /// 상호작용 가능 범위 (미터)
        /// </summary>
        public float InteractionRange;
    }
}