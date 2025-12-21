using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 상호작용 가능한 오브젝트
    /// </summary>
    public struct InteractableComponent : IComponentData
    {
        public bool IsActive;
    }

    /// <summary>
    /// 상호작용 요청 플레이어에부착
    /// </summary>
    public struct InteractionRequestComponent : IComponentData
    {
        public Entity TargetStation;
    }




    public struct TrashCanRequestTag : IComponentData { }
    public struct ContainerRequestTag : IComponentData { }
    public struct StoveRequestTag : IComponentData { }
    public struct CuttingBoardRequestTag : IComponentData { }
    public struct CounterRequestTag : IComponentData { }
    public struct ServingRequestTag : IComponentData { }

    //쓰레기통, 작업대, 뭐더있지
}