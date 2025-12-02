using Unity.Entities;
using Unity.Mathematics;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 스토브 설정 (기계 정보)
    /// </summary>
    public struct StoveComponent : IComponentData
    {
        /// <summary>조리 속도 배율 (1.0 = 정상, 2.0 = 2배 빠름)</summary>
        public float CookingSpeedMultiplier;

        // CookDuration은 아이템(CookableComponent)에 있으니 여기선 뺍니다.
    }

    /// <summary>
    /// 현재 스토브의 상태 (런타임)
    /// </summary>
    public struct StoveCookingState : IComponentData
    {
        public Entity ItemEntity; // 현재 올려져 있는 아이템
        public float CurrentCookProgress; // 현재 얼마나 구워졌나 (누적 시간)
        public bool IsCooking; // 지금 불이 켜져 있나?
    }

    /// <summary>
    /// [추가] 아이템이 놓일 시각적 위치
    /// </summary>
    public struct StoveSnapPoint : IComponentData
    {
        public float3 LocalPosition;
    }
}