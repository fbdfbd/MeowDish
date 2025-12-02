using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Authoring
{
    public class ServingCounterAuthoring : MonoBehaviour
    {
        // =========================================================
        // 1. 인스펙터 설정
        // =========================================================
        [Header("서빙 설정")]
        [Tooltip("최대 대기 인원 (줄 서는 사람 수)")]
        public int maxQueueSize = 3;

        [Header("위치 설정")]
        [Tooltip("손님이 주문하려고 서는 위치 (계산대 바로 앞)")]
        public Transform queueStartPoint;

        [Header("스테이션 설정")]
        public int stationID = 0;

        [Header("물리 설정")]
        public Vector3 colliderSize = new Vector3(1.5f, 1.0f, 1.5f);

        // =========================================================
        // 2. 내부 변수
        // =========================================================
        private Entity _servingEntity;
        private EntityManager _entityManager;

        // =========================================================
        // 3. 초기화
        // =========================================================
        private void Start()
        {
            InitializeEntityManager();

            if (stationID == 0) stationID = gameObject.GetInstanceID();

            CreateEntity();

            SetupTransform();
            SetupGameLogicComponents();
            SetupPhysics();
        }

        private void InitializeEntityManager()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void CreateEntity()
        {
            _servingEntity = _entityManager.CreateEntity();
#if UNITY_EDITOR
            _entityManager.SetName(_servingEntity, $"ServingCounter_{stationID}");
#endif
        }

        private void SetupTransform()
        {
            var position = transform.position;
            _entityManager.AddComponentData(_servingEntity, LocalTransform.FromPosition(position));
            _entityManager.AddComponentData(_servingEntity, new LocalToWorld
            {
                Value = float4x4.TRS(position, quaternion.identity, new float3(1))
            });
        }

        private void SetupGameLogicComponents()
        {
            // 1. Station Component
            _entityManager.AddComponentData(_servingEntity, new StationComponent
            {
                Type = StationType.ServingCounter, // Enum에 ServingCounter 확인!
                StationID = stationID,
                PlacedItemEntity = Entity.Null
            });

            // 2. Interactable
            _entityManager.AddComponentData(_servingEntity, new InteractableComponent
            {
                IsActive = true
            });

            // 3. Serving Station Component (대기열 정보)
            _entityManager.AddComponentData(_servingEntity, new ServingStationComponent
            {
                MaxQueueCapacity = maxQueueSize,
                CurrentQueueCount = 0,
                CurrentCustomer = Entity.Null // 아직 손님 없음
            });

            // 4. [중요] 줄 서는 위치 (SnapPoint 재활용 또는 새로 정의)
            // 여기서는 손님이 설 위치를 저장하기 위해 별도 컴포넌트나 SnapPoint를 씁니다.
            // 편의상 기존에 만들어둔 CounterSnapPoint나 StoveSnapPoint 말고,
            // ServingPoint를 따로 만들어도 되지만, 일단 LocalTransform 위치를 기준으로 계산해도 됩니다.
            // 하지만 정확한 위치를 위해 LocalPosition을 저장해둡니다.

            float3 queueLocalPos = new float3(0, 0, 1.5f); // 기본값: 카운터 앞 1.5m
            if (queueStartPoint != null) queueLocalPos = queueStartPoint.localPosition;

            // *팁: ServingPoint 컴포넌트가 없다면 만드셔도 되고, 
            // 아니면 그냥 StationComponent에 위치 정보를 넣을 순 없으니..
            // 간단하게 '손님용 위치' 컴포넌트를 하나 붙입시다.
            _entityManager.AddComponentData(_servingEntity, new ServingQueuePoint
            {
                StartLocalPosition = queueLocalPos,
                QueueInterval = 1.0f // 뒷사람과의 간격 (1m)
            });
        }

        private void SetupPhysics()
        {
            var boxGeometry = new BoxGeometry
            {
                Center = new float3(0, 0, 0),
                Orientation = quaternion.identity,
                Size = new float3(colliderSize.x, colliderSize.y, colliderSize.z),
                BevelRadius = 0.05f
            };

            var collider = Unity.Physics.BoxCollider.Create(
                boxGeometry,
                new CollisionFilter
                {
                    BelongsTo = 1u << 6,
                    CollidesWith = ~0u,
                    GroupIndex = 0
                }
            );

            _entityManager.AddComponentData(_servingEntity, new PhysicsCollider { Value = collider });
            _entityManager.AddComponentData(_servingEntity, new PhysicsVelocity());
            _entityManager.AddComponentData(_servingEntity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
            _entityManager.AddComponent<Simulate>(_servingEntity);
            _entityManager.AddSharedComponent(_servingEntity, new PhysicsWorldIndex(0));
        }

        private void LateUpdate()
        {
            if (_entityManager.Exists(_servingEntity))
            {
                var lt = _entityManager.GetComponentData<LocalTransform>(_servingEntity);
                transform.position = lt.Position;
            }
        }

        private void OnDestroy()
        {
            if (World.DefaultGameObjectInjectionWorld == null) return;
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (em.Exists(_servingEntity))
            {
                if (em.HasComponent<PhysicsCollider>(_servingEntity))
                {
                    var col = em.GetComponentData<PhysicsCollider>(_servingEntity);
                    if (col.Value.IsCreated) col.Value.Dispose();
                }
                em.DestroyEntity(_servingEntity);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta; // 서빙 카운터는 핑크색
            Gizmos.DrawWireCube(transform.position + new Vector3(0, 0.5f, 0), colliderSize);

            // 대기열 위치 표시
            if (queueStartPoint != null)
            {
                Gizmos.color = Color.green;
                Vector3 startPos = queueStartPoint.position;
                Gizmos.DrawSphere(startPos, 0.2f);

                // 뒤로 줄 서는 모습 가이드
                for (int i = 1; i < maxQueueSize; i++)
                {
                    // 카운터 정면 방향으로 줄을 선다고 가정 (로컬 Z축)
                    // 실제로는 Authoring에서 방향도 설정해주면 좋음. 여기선 단순화.
                    Vector3 nextPos = startPos + (transform.forward * i * 1.0f);
                    Gizmos.DrawWireSphere(nextPos, 0.2f);
                }
            }
        }
    }
}