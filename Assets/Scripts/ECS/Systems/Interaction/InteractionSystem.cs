using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    /// <summary>상호작용 입력 감지 및 요청 생성</summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct InteractionSystem : ISystem
    {
        [BurstCompile] public void OnCreate(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<GamePauseComponent>(out var pause) && pause.IsPaused) return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (playerInput, playerState, entity) in
                     SystemAPI.Query<RefRO<PlayerInputComponent>, RefRO<PlayerStateComponent>>()
                              .WithEntityAccess())
            {
                if (!playerInput.ValueRO.InteractTapped) continue;
                if (SystemAPI.HasComponent<InteractionRequestComponent>(entity)) continue;
                if (!playerState.ValueRO.IsNearStation) continue;

                Entity stationEntity = playerState.ValueRO.CurrentStationEntity;
                if (stationEntity == Entity.Null) continue;
                if (!SystemAPI.HasComponent<StationComponent>(stationEntity)) continue;

                var station = SystemAPI.GetComponent<StationComponent>(stationEntity);

                ecb.AddComponent(entity, new InteractionRequestComponent
                {
                    TargetStation = stationEntity
                });

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
                        ecb.RemoveComponent<InteractionRequestComponent>(entity);
                        break;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
