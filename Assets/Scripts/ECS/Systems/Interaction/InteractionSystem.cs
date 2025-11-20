using Unity.Entities;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// 상호작용 입력 처리 시스템
    /// 
    /// 역할:
    /// 1. PlayerInputComponent.InteractTapped 감지
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
            var ecb = _ecbSystem.CreateCommandBuffer();
            Debug.Log($"[InteractionSystem] 실행중。。。");
            // 플레이어 찾기 (입력 + 상태 둘 다 필요)
            foreach (var (playerInput, playerState, playerEntity) in
                     SystemAPI.Query<RefRO<PlayerInputComponent>, RefRO<PlayerStateComponent>>()
                     .WithEntityAccess())
            {
                // 상호작용 입력이 없으면 무시
                if (!playerInput.ValueRO.InteractTapped) continue;

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