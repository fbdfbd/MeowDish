using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 스토브에서 구울 수 있는 아이템
    /// </summary>
    public struct CookableComponent : IComponentData
    {
        public float CookTime;   // 완전히 익는 데 걸리는 시간
        public float BurnTime;   // 0이면 안탐
    }

    /// <summary>
    /// 현재 조리 진행 상태
    /// </summary>
    public struct CookingState : IComponentData
    {
        public float Elapsed;    // 지금까지 구운 시간
    }

    /// <summary>
    /// 탈 수 있는 아이템 태그
    /// </summary>
    public struct BurnableTag : IComponentData { }
}