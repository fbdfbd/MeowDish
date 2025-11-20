using Unity.Entities;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// 상호작용 입력 처리 시스템
    /// 
    /// 역할:
    /// 1. E키 또는 상호작용 버튼 입력 감지
    /// 2. PlayerState.CurrentStationEntity 확인
    /// 3. InteractionRequestComponent 생성
    /// 
    /// Physics 판정은 StationRaycastDetectionSystem이 처리!
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class InteractionSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            // E키 입력 체크 (TODO: InputSystem에서 처리)
            bool interactPressed = Input.GetKeyDown(KeyCode.E);

            if (!interactPressed) return;

            var ecb = _ecbSystem.CreateCommandBuffer();

            // 플레이어 찾기
            foreach (var (playerState, playerEntity) in
                     SystemAPI.Query<RefRO<PlayerStateComponent>>()
                     .WithEntityAccess())
            {
                // 스테이션 근처가 아니면 무시
                if (!playerState.ValueRO.IsNearStation) continue;

                Entity stationEntity = playerState.ValueRO.CurrentStationEntity;
                if (stationEntity == Entity.Null) continue;

                // 스테이션 타입 확인
                if (!SystemAPI.HasComponent<StationComponent>(stationEntity))
                    continue;

                var station = SystemAPI.GetComponent<StationComponent>(stationEntity);

                // 상호작용 요청 생성
                Entity requestEntity = ecb.CreateEntity();
                ecb.AddComponent(requestEntity, new InteractionRequestComponent
                {
                    PlayerEntity = playerEntity,
                    StationEntity = stationEntity,
                    StationType = station.Type
                });

                Debug.Log($"[상호작용 요청: {station.Type}]");
            }
        }
    }
}