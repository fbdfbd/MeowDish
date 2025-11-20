using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 상호작용 요청 이벤트
    /// InteractionSystem이 생성하고, 각 세부 시스템이 소비
    /// </summary>
    public struct InteractionRequestComponent : IComponentData
    {
        public Entity PlayerEntity;
        public Entity StationEntity;
        public StationType StationType;
    }
}