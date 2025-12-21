using Unity.Entities;
using Unity.Mathematics;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 스토브 설정
    /// </summary>
    public struct StoveComponent : IComponentData
    {
        /// <summary>조리 속도 배율</summary>
        public float CookingSpeedMultiplier;
    }

    /// <summary>
    /// 현재 스토브의 상태
    /// </summary>
    public struct StoveCookingState : IComponentData
    {
        public Entity ItemEntity;            // 현재 올려져 있는 아이템
        public float CurrentCookProgress;    // 누적 조리 시간
        public bool IsCooking;
        public int SfxLoopHandle;            // 그릴링 루프 사운드 핸들
        public int SmokeLoopHandle;
    }

    /// <summary>
    /// 아이템 시각적 위치
    /// </summary>
    public struct StoveSnapPoint : IComponentData
    {
        public float3 LocalPosition;
    }
}
