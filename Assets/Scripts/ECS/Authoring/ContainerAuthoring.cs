using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Authoring
{
    public class ContainerAuthoring : MonoBehaviour
    {
        [Header("컨테이너 설정")]
        public IngredientType providedIngredient = IngredientType.Bread;
        public bool allowReturn = true;
        public bool isInfinite = true;

        [Header("스테이션 설정")]
        public int stationID = 0;

        [Header("상호작용 설정")]
        public float interactionRange = 2.0f;

        private Entity _containerEntity;
        private EntityManager _entityManager;

        private void Start()
        {
            Debug.Log($"========== ContainerAuthoring Start ==========");

            var world = World.DefaultGameObjectInjectionWorld;

            if (world == null || !world.IsCreated)
            {
                Debug.LogError("[Container] ? World 없음!");
                return;
            }

            _entityManager = world.EntityManager;
            _containerEntity = _entityManager.CreateEntity();

            var position = transform.position;

            Debug.Log($"[Container] Entity: {_containerEntity}");
            Debug.Log($"[Container] Position: {position}");

            // 1. LocalTransform
            _entityManager.AddComponentData(_containerEntity,
                LocalTransform.FromPosition(position));

            // 2. LocalToWorld
            _entityManager.AddComponentData(_containerEntity, new LocalToWorld
            {
                Value = float4x4.TRS(position, quaternion.identity, new float3(1))
            });

            // 3-5. Game Components
            _entityManager.AddComponentData(_containerEntity, new StationComponent
            {
                Type = StationType.Container,
                StationID = stationID,
                PlacedItemEntity = Entity.Null
            });

            _entityManager.AddComponentData(_containerEntity, new ContainerComponent
            {
                ProvidedIngredient = providedIngredient,
                AllowReturn = allowReturn,
                IsInfinite = isInfinite
            });

            _entityManager.AddComponentData(_containerEntity, new InteractableComponent
            {
                IsActive = true,
                InteractionRange = interactionRange
            });

            // 6. PhysicsCollider
            var collider = Unity.Physics.BoxCollider.Create(
                new Unity.Physics.BoxGeometry
                {
                    Center = float3.zero,
                    Orientation = quaternion.identity,
                    Size = new float3(1.5f, 2f, 1.5f),
                    BevelRadius = 0.05f
                },
                new Unity.Physics.CollisionFilter
                {
                    BelongsTo = 1u << 6,
                    CollidesWith = ~0u,
                    GroupIndex = 0
                }
            );

            _entityManager.AddComponentData(_containerEntity, new PhysicsCollider
            {
                Value = collider
            });

            // 7. PhysicsVelocity
            _entityManager.AddComponentData(_containerEntity, new PhysicsVelocity
            {
                Linear = float3.zero,
                Angular = float3.zero
            });

            // 8. PhysicsMass
            _entityManager.AddComponentData(_containerEntity,
                PhysicsMass.CreateKinematic(MassProperties.UnitSphere));

            // ??? 9. Simulate!
            _entityManager.AddComponent<Simulate>(_containerEntity);
            Debug.Log("[Container] ? Simulate");

            // ??? 10. PhysicsWorldIndex! (최종 해결!)
            _entityManager.AddSharedComponent(_containerEntity, new PhysicsWorldIndex(0));
            Debug.Log("[Container] ??? PhysicsWorldIndex");

            Debug.Log($"========== Container 완료 ==========");
        }


        private void LateUpdate()
        {
            if (_entityManager.Exists(_containerEntity))
            {
                var lt = _entityManager.GetComponentData<LocalTransform>(_containerEntity);
                transform.position = lt.Position;
            }
        }

        private void OnDestroy()
        {
            if (World.DefaultGameObjectInjectionWorld == null ||
                !World.DefaultGameObjectInjectionWorld.IsCreated)
                return;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (em.Exists(_containerEntity))
            {
                if (em.HasComponent<PhysicsCollider>(_containerEntity))
                {
                    var collider = em.GetComponentData<PhysicsCollider>(_containerEntity);
                    if (collider.Value.IsCreated)
                    {
                        collider.Value.Dispose();
                    }
                }

                em.DestroyEntity(_containerEntity);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 2.0f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, new Vector3(1.5f, 2f, 1.5f));
        }
    }
}
