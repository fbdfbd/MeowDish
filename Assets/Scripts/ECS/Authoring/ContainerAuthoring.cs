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
        // =========================================================
        // 1. 인스펙터 설정
        // =========================================================
        [Header("컨테이너 설정")]
        public IngredientType providedIngredient = IngredientType.Bread;
        public bool allowReturn = true;
        public bool isInfinite = true;

        [Header("스테이션 설정")]
        public int stationID = 0;

        [Header("물리 설정")]
        public Vector3 colliderSize = new Vector3(1.5f, 2f, 1.5f);

        // =========================================================
        // 2. 내부 변수
        // =========================================================
        private Entity _containerEntity;
        private EntityManager _entityManager;

        // =========================================================
        // 3. 초기화 (목차)
        // =========================================================
        private void Start()
        {
            if (!InitializeEntityManager()) return;

            CreateContainerEntity();

            SetupTransform();           // 위치, 회전
            SetupGameLogicComponents(); // Station, Container, Interactable
            SetupPhysics();             // 물리 (Collider, Rigidbody)
        }

        // =========================================================
        // 4. 세부 설정 메서드들
        // =========================================================

        private bool InitializeEntityManager()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogError("[Container] DefaultGameObjectInjectionWorld가 없습니다!");
                return false;
            }

            _entityManager = world.EntityManager;
            return true;
        }

        private void CreateContainerEntity()
        {
            _containerEntity = _entityManager.CreateEntity();

#if UNITY_EDITOR
            _entityManager.SetName(_containerEntity, $"Container_{providedIngredient}_{stationID}");
#endif
        }

        private void SetupTransform()
        {
            var position = transform.position;

            _entityManager.AddComponentData(_containerEntity, LocalTransform.FromPosition(position));
            _entityManager.AddComponentData(_containerEntity, new LocalToWorld
            {
                Value = float4x4.TRS(position, quaternion.identity, new float3(1))
            });
        }

        private void SetupGameLogicComponents()
        {
            // 1. Station Component
            _entityManager.AddComponentData(_containerEntity, new StationComponent
            {
                Type = StationType.Container,
                StationID = stationID,
                PlacedItemEntity = Entity.Null
            });

            // 2. Container Component
            _entityManager.AddComponentData(_containerEntity, new ContainerComponent
            {
                ProvidedIngredient = providedIngredient,
                AllowReturn = allowReturn,
                IsInfinite = isInfinite
            });

            // 3. Interactable Component
            _entityManager.AddComponentData(_containerEntity, new InteractableComponent
            {
                IsActive = true
            });
        }

        private void SetupPhysics()
        {
            // Box Geometry 생성
            var boxGeometry = new BoxGeometry
            {
                Center = float3.zero,
                Orientation = quaternion.identity,
                Size = new float3(colliderSize.x, colliderSize.y, colliderSize.z),
                BevelRadius = 0.05f
            };

            // Collider 생성 (Layer 6)
            var collider = Unity.Physics.BoxCollider.Create(
                boxGeometry,
                new CollisionFilter
                {
                    BelongsTo = 1u << 6, // 예: Interactable Layer
                    CollidesWith = ~0u,
                    GroupIndex = 0
                }
            );

            _entityManager.AddComponentData(_containerEntity, new PhysicsCollider { Value = collider });
            _entityManager.AddComponentData(_containerEntity, new PhysicsVelocity()); // 정적이지만 필요할 수 있음
            _entityManager.AddComponentData(_containerEntity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
            _entityManager.AddComponent<Simulate>(_containerEntity);
            _entityManager.AddSharedComponent(_containerEntity, new PhysicsWorldIndex(0));
        }

        // =========================================================
        // 5. 업데이트 및 해제
        // =========================================================

        private void LateUpdate()
        {
            // 엔티티 위치를 게임오브젝트에 동기화 (필요시)
            if (_entityManager.Exists(_containerEntity))
            {
                var lt = _entityManager.GetComponentData<LocalTransform>(_containerEntity);
                transform.position = lt.Position;
            }
        }

        private void OnDestroy()
        {
            DisposeContainerResources();
        }

        private void DisposeContainerResources()
        {
            if (World.DefaultGameObjectInjectionWorld == null || !World.DefaultGameObjectInjectionWorld.IsCreated)
                return;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (em.Exists(_containerEntity))
            {
                // Collider 메모리 해제
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

        // =========================================================
        // 6. 디버그 (Gizmos)
        // =========================================================
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, colliderSize);
        }
    }
}