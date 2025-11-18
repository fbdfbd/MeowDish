using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// 플레이어-스테이션 상호작용 감지
    /// 
    /// 작동:
    /// 1. 플레이어 주변 스테이션 찾기
    /// 2. E 키 누르면 StationType에 따라 처리
    /// 3. Container/WorkBench 등으로 분기
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMovementSystem))]
    public partial class InteractionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // ????????????????????????????????????????
            // 방법 1: EntityQuery 사용
            // ????????????????????????????????????????
            var stationQuery = GetEntityQuery(
                typeof(LocalTransform),
                typeof(InteractableComponent),
                typeof(StationComponent)
            );

            var stations = stationQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            Entities
                .WithAll<PlayerStateComponent>()
                .ForEach((
                    Entity playerEntity,
                    ref PlayerStateComponent playerState,
                    in LocalTransform playerTransform,
                    in PlayerInputComponent input) =>
                {
                    Entity closestStation = Entity.Null;
                    float closestDistance = float.MaxValue;

                    // 모든 스테이션 순회
                    foreach (var stationEntity in stations)
                    {
                        var interactable = SystemAPI.GetComponent<InteractableComponent>(stationEntity);

                        if (!interactable.IsActive)
                            continue;

                        var stationTransform = SystemAPI.GetComponent<LocalTransform>(stationEntity);

                        // 거리 계산 (XZ 평면만, Y 무시)
                        float3 playerPos = playerTransform.Position;
                        float3 stationPos = stationTransform.Position;
                        float distance = math.distance(
                            new float2(playerPos.x, playerPos.z),
                            new float2(stationPos.x, stationPos.z)
                        );

                        // 범위 내 + 가장 가까운 것
                        if (distance <= interactable.InteractionRange &&
                            distance < closestDistance)
                        {
                            closestStation = stationEntity;
                            closestDistance = distance;
                        }
                    }

                    // 플레이어 상태 업데이트
                    playerState.CurrentStationEntity = closestStation;
                    playerState.IsNearStation = closestStation != Entity.Null;

                    // ????????????????????????????????????????
                    // 2. E 키 감지
                    // ????????????????????????????????????????
                    if (input.InteractPressed && playerState.IsNearStation)
                    {
                        // 스테이션 타입 확인
                        var station = SystemAPI.GetComponent<StationComponent>(closestStation);

                        // 타입별 처리는 각 System에서
                        // 여기서는 감지만!
                        UnityEngine.Debug.Log($"[InteractionSystem] Interacting with {station.Type}");
                    }

                }).WithoutBurst().Run();

            stations.Dispose();
        }
    }
}