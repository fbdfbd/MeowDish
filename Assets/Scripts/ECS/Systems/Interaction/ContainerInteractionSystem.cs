using Unity.Entities;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// 컨테이너 상호작용 시스템 (테스트용 - 디버그만)
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(StationRaycastDetectionSystem))]  // 이건 같은 그룹이니까 OK!
    public partial class ContainerInteractionSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (request, requestEntity) in
                     SystemAPI.Query<RefRO<InteractionRequestComponent>>()
                     .WithEntityAccess())
            {
                if (request.ValueRO.StationType != StationType.Container)
                    continue;

                Entity playerEntity = request.ValueRO.PlayerEntity;
                Entity stationEntity = request.ValueRO.StationEntity;

                var playerState = SystemAPI.GetComponentRW<PlayerStateComponent>(playerEntity);
                var container = SystemAPI.GetComponent<ContainerComponent>(stationEntity);

                Debug.Log("========================================");
                Debug.Log($"[컨테이너 상호작용 성공!]");
                Debug.Log($"제공 재료: {container.ProvidedIngredient}");
                Debug.Log($"반납 허용: {container.AllowReturn}");
                Debug.Log($"무한 제공: {container.IsInfinite}");
                Debug.Log($"플레이어가 아이템 들고 있음: {playerState.ValueRO.IsHoldingItem}");

                if (!playerState.ValueRO.IsHoldingItem)
                {
                    Debug.Log($"[동작] {container.ProvidedIngredient} 가져가기 (아이템 생성 - 미구현)");
                }
                else
                {
                    Debug.Log($"[동작] 아이템 반납 시도 (미구현)");
                }
                Debug.Log("========================================");

                ecb.DestroyEntity(requestEntity);
            }
        }
    }
}