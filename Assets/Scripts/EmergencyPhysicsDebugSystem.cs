using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using Meow.ECS.Components;
using Unity.Transforms;

namespace Meow.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct EmergencyPhysicsDebugSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<PhysicsWorldSingleton>(out var physicsWorldSingleton))
            {
                Debug.LogWarning("PhysicsWorld 준비 안 됨!");
                return;
            }

            var physicsWorld = physicsWorldSingleton.PhysicsWorld;

            Debug.Log($"========== PHYSICS DEBUG ==========");
            Debug.Log($"PhysicsWorld Bodies: {physicsWorld.NumBodies}");

            // PhysicsWorld의 모든 Body 출력
            for (int i = 0; i < physicsWorld.NumBodies; i++)
            {
                var body = physicsWorld.Bodies[i];
                Debug.Log($"Body[{i}]: Entity={body.Entity}, Pos={body.WorldFromBody.pos}");
            }

            // 모든 Station Entity 출력
            Debug.Log($"--- Station Entities ---");

            var em = state.EntityManager;

            foreach (var (station, entity) in
                     SystemAPI.Query<RefRO<StationComponent>>().WithEntityAccess())
            {
                bool hasCollider = em.HasComponent<PhysicsCollider>(entity);
                bool hasL2W = em.HasComponent<LocalToWorld>(entity);
                bool hasSimulate = em.HasComponent<Simulate>(entity);  // ? Simulate 확인!

                Debug.Log($"Station {entity}:");
                Debug.Log($"  Collider={hasCollider}");
                Debug.Log($"  LocalToWorld={hasL2W}");
                Debug.Log($"  Simulate={hasSimulate}");  // ? 추가!
            }

            Debug.Log($"===================================");

            // 한 번만 실행
            state.Enabled = false;
        }
    }
}
