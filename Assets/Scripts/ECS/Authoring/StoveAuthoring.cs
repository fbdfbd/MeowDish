using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Authoring
{
    public class StoveAuthoring : MonoBehaviour
    {
        // =========================================================
        // 1. 인스펙터 설정
        // =========================================================
        [Header("스토브 설정")]
        public float cookingSpeed = 1.0f;

        [Header("시각적 위치 설정")]
        [Tooltip("프라이팬/아이템이 놓일 위치 (빈 GameObject)")]
        public Transform snapPoint;

        [Header("스테이션 설정")]
        public int stationID = 0;

        [Header("물리 설정")]
        public Vector3 colliderSize = new Vector3(1.5f, 1.0f, 1.5f); // 스토브 크기에 맞춰 조절

        // =========================================================
        // 2. 내부 변수
        // =========================================================
        private Entity _stoveEntity;
        private EntityManager _entityManager;

        // =========================================================
        // 3. 초기화
        // =========================================================
        private void Start()
        {
            InitializeEntityManager();

            if (stationID == 0) stationID = gameObject.GetInstanceID();

            CreateStoveEntity();

            SetupTransform();
            SetupGameLogicComponents();
            SetupPhysics();
        }

        private void InitializeEntityManager()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void CreateStoveEntity()
        {
            _stoveEntity = _entityManager.CreateEntity();
#if UNITY_EDITOR
            _entityManager.SetName(_stoveEntity, $"Stove_{stationID}");
#endif
        }

        private void SetupTransform()
        {
            var position = transform.position;
            _entityManager.AddComponentData(_stoveEntity, LocalTransform.FromPosition(position));
            _entityManager.AddComponentData(_stoveEntity, new LocalToWorld
            {
                Value = float4x4.TRS(position, quaternion.identity, new float3(1))
            });
        }

        private void SetupGameLogicComponents()
        {
            // 1. Station (Type: Stove 확인!)
            _entityManager.AddComponentData(_stoveEntity, new StationComponent
            {
                Type = StationType.Stove, // ?? Enum에 Stove가 있어야 함 (값: 4)
                StationID = stationID,
                PlacedItemEntity = Entity.Null
            });

            // 2. Interactable
            _entityManager.AddComponentData(_stoveEntity, new InteractableComponent
            {
                IsActive = true
            });

            // 3. Stove Data
            _entityManager.AddComponentData(_stoveEntity, new StoveComponent
            {
                CookingSpeedMultiplier = cookingSpeed
            });

            // 4. Stove State (초기화)
            _entityManager.AddComponentData(_stoveEntity, new StoveCookingState
            {
                ItemEntity = Entity.Null,
                CurrentCookProgress = 0f,
                IsCooking = false
            });

            // 5. Snap Point
            float3 snapLocalPos = new float3(0, 1.0f, 0); // 기본값
            if (snapPoint != null) snapLocalPos = snapPoint.localPosition;

            _entityManager.AddComponentData(_stoveEntity, new StoveSnapPoint
            {
                LocalPosition = snapLocalPos
            });
        }

        private void SetupPhysics()
        {
            // 박스 크기 및 오프셋
            var boxGeometry = new BoxGeometry
            {
                Center = new float3(0, 0f, 0),
                Orientation = quaternion.identity,
                Size = new float3(colliderSize.x, colliderSize.y, colliderSize.z),
                BevelRadius = 0.05f
            };

            // ?? [중요] 레이어 설정 (Raycast 필터와 일치해야 함!)
            // RaycastSystem: CollidesWith = 1u << 6
            // Here: BelongsTo = 1u << 6
            var collider = Unity.Physics.BoxCollider.Create(
                boxGeometry,
                new CollisionFilter
                {
                    BelongsTo = 1u << 6, // 6번 비트 (Interactable Layer)
                    CollidesWith = ~0u,
                    GroupIndex = 0
                }
            );

            _entityManager.AddComponentData(_stoveEntity, new PhysicsCollider { Value = collider });
            _entityManager.AddComponentData(_stoveEntity, new PhysicsVelocity());
            _entityManager.AddComponentData(_stoveEntity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
            _entityManager.AddComponent<Simulate>(_stoveEntity); // 물리 세계에 등록
            _entityManager.AddSharedComponent(_stoveEntity, new PhysicsWorldIndex(0));
        }

        private void LateUpdate()
        {
            if (_entityManager.Exists(_stoveEntity))
            {
                var lt = _entityManager.GetComponentData<LocalTransform>(_stoveEntity);
                transform.position = lt.Position;
            }
        }

        private void OnDestroy()
        {
            if (World.DefaultGameObjectInjectionWorld == null) return;
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (em.Exists(_stoveEntity))
            {
                if (em.HasComponent<PhysicsCollider>(_stoveEntity))
                {
                    var col = em.GetComponentData<PhysicsCollider>(_stoveEntity);
                    if (col.Value.IsCreated) col.Value.Dispose();
                }
                em.DestroyEntity(_stoveEntity);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (this == null || gameObject == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + new Vector3(0, 0.5f, 0), colliderSize);

            if (snapPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(snapPoint.position, 0.2f);
            }
        }
    }
}