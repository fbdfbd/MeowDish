using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// 상호작용 입력 감지 및 요청 생성 시스템
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct InteractionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerInputComponent>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 1) 일시정지 체크
            if (SystemAPI.TryGetSingleton<GamePauseComponent>(out var pause))
            {
                if (pause.IsPaused) return;
            }

            // 2) 후처리 커맨드 버퍼(싱글톤)
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // 3) 플레이어 순회
            foreach (var (playerInput, playerState, entity) in
                     SystemAPI.Query<RefRO<PlayerInputComponent>, RefRO<PlayerStateComponent>>()
                     .WithEntityAccess())
            {
                // 입력 및 상태 체크
                if (!playerInput.ValueRO.InteractTapped) continue;
                if (SystemAPI.HasComponent<InteractionRequestComponent>(entity)) continue; // 이미 요청 중이면 패스
                if (!playerState.ValueRO.IsNearStation) continue;

                Entity stationEntity = playerState.ValueRO.CurrentStationEntity;
                if (stationEntity == Entity.Null) continue;

                if (!SystemAPI.HasComponent<StationComponent>(stationEntity)) continue;

                var station = SystemAPI.GetComponent<StationComponent>(stationEntity);

                // 4) 공통 요청 컴포넌트 추가 예약
                ecb.AddComponent(entity, new InteractionRequestComponent
                {
                    TargetStation = stationEntity
                });

                // 5) 스테이션 타입별 태그 추가 예약
                switch (station.Type)
                {
                    case StationType.Counter:
                        ecb.AddComponent<CounterRequestTag>(entity);
                        break;
                    case StationType.Container:
                        ecb.AddComponent<ContainerRequestTag>(entity);
                        break;
                    case StationType.Stove:
                        ecb.AddComponent<StoveRequestTag>(entity);
                        break;
                    case StationType.CuttingBoard:
                        ecb.AddComponent<CuttingBoardRequestTag>(entity);
                        break;
                    case StationType.TrashCan:
                        ecb.AddComponent<TrashCanRequestTag>(entity);
                        break;
                    case StationType.ServingCounter:
                        ecb.AddComponent<ServingRequestTag>(entity);
                        break;
                    default:
                        // 정의되지 않은 타입이라면 요청 자체를 취소
                        ecb.RemoveComponent<InteractionRequestComponent>(entity);
                        break;
                }
            }
        }
    }
}