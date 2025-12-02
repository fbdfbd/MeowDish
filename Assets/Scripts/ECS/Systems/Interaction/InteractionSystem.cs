using Unity.Entities;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// 상호작용 입력 감지 및 요청 생성 시스템
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

            // 플레이어 찾기 (입력 + 상태 필요)
            foreach (var (playerInput, playerState, playerEntity) in
                     SystemAPI.Query<RefRO<PlayerInputComponent>, RefRO<PlayerStateComponent>>()
                     .WithEntityAccess())
            {
                // 1. 입력 확인
                if (!playerInput.ValueRO.InteractTapped) continue;

                // 2. 중복 방지
                if (SystemAPI.HasComponent<InteractionRequestComponent>(playerEntity)) continue;

                // 3. 스테이션 확인
                if (!playerState.ValueRO.IsNearStation) continue;

                Entity stationEntity = playerState.ValueRO.CurrentStationEntity;
                if (stationEntity == Entity.Null) continue;

                // 4. 스테이션 컴포넌트 확인
                if (!SystemAPI.HasComponent<StationComponent>(stationEntity)) continue;

                var station = SystemAPI.GetComponent<StationComponent>(stationEntity);

                // =================================================
                // 5. 요청 포스트잇 붙이기
                // =================================================

                // [데이터] 공통 요청 정보
                ecb.AddComponent(playerEntity, new InteractionRequestComponent
                {
                    TargetStation = stationEntity
                });

                // [태그] 스테이션 타입별 분류 (Switch)
                switch (station.Type)
                {
                    // ?? [추가됨] 카운터 (WorkBench 또는 Counter)
                    case StationType.Counter:
                        ecb.AddComponent<CounterRequestTag>(playerEntity);
                        Debug.Log($"[Interaction] 카운터 요청 생성 -> {stationEntity}");
                        break;

                    case StationType.Container:
                        ecb.AddComponent<ContainerRequestTag>(playerEntity);
                        Debug.Log($"[Interaction] 컨테이너 요청 생성 -> {stationEntity}");
                        break;

                    case StationType.Stove:
                        ecb.AddComponent<StoveRequestTag>(playerEntity);
                        Debug.Log($"[Interaction] 스토브 요청 생성 -> {stationEntity}");
                        break;

                    case StationType.CuttingBoard:
                        ecb.AddComponent<CuttingBoardRequestTag>(playerEntity);
                        Debug.Log($"[Interaction] 도마 요청 생성 -> {stationEntity}");
                        break;

                    case StationType.TrashCan:
                        ecb.AddComponent<TrashCanRequestTag>(playerEntity);
                        Debug.Log($"[Interaction] 쓰레기통 요청 생성 -> {stationEntity}");
                        break;
                    case StationType.ServingCounter:
                        ecb.AddComponent<ServingRequestTag>(playerEntity);
                        Debug.Log($"[Interaction] 서빙카운터 요청 생성 -> {stationEntity}");
                        break;


                    default:
                        Debug.LogWarning($"[Interaction] 처리할 수 없는 스테이션 타입: {station.Type}");
                        // 알 수 없는 타입이면 요청 취소 (안 붙임)
                        // 이미 붙인 RequestComponent도 떼는 게 안전함
                        ecb.RemoveComponent<InteractionRequestComponent>(playerEntity);
                        break;
                }
            }
        }
    }
}