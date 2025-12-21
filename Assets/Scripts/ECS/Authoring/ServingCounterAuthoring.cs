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
        [Header("서빙 설정")]
        [Tooltip("최대 대기 인원")]
        public int maxQueueSize = 3;

        [Header("위치 설정")]
        [Tooltip("손님이 주문하려고 서는 위치")]
        public Transform queueStartPoint;

        [Header("스테이션 설정")]
        public int stationID = 0;

        [Header("물리 설정")]
        public Vector3 colliderSize = new Vector3(1.5f, 1.0f, 1.5f);


        private Entity _servingEntity;
        private EntityManager _entityManager;

        private BlobAssetReference<Unity.Physics.Collider> _colliderBlobRef;


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
            _entityManager.AddComponentData(_servingEntity, new StationComponent
            {
                Type = StationType.ServingCounter,
                StationID = stationID,
                PlacedItemEntity = Entity.Null
            });

            _entityManager.AddComponentData(_servingEntity, new InteractableComponent
            {
                IsActive = true
            });

            _entityManager.AddComponentData(_servingEntity, new ServingStationComponent
            {
                MaxQueueCapacity = maxQueueSize,
                CurrentQueueCount = 0,
                CurrentCustomer = Entity.Null
            });

            float3 queueLocalPos = new float3(0, 0, 1f); // 기본값: 카운터 앞 1.5m
            if (queueStartPoint != null) queueLocalPos = queueStartPoint.localPosition;

            _entityManager.AddComponentData(_servingEntity, new ServingQueuePoint
            {
                StartLocalPosition = queueLocalPos,
                QueueInterval = 1.0f
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

            _colliderBlobRef = Unity.Physics.BoxCollider.Create(
                boxGeometry,
                new CollisionFilter
                {
                    BelongsTo = 1u << 6,
                    CollidesWith = ~0u,
                    GroupIndex = 0
                }
            );

            _entityManager.AddComponentData(_servingEntity, new PhysicsCollider { Value = _colliderBlobRef });

            _entityManager.AddComponentData(_servingEntity, new PhysicsVelocity());
            _entityManager.AddComponentData(_servingEntity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
            _entityManager.AddComponent<Simulate>(_servingEntity);
            _entityManager.AddSharedComponent(_servingEntity, new PhysicsWorldIndex(0));
        }

        public Entity GetEntity()
        {
            if (_entityManager != default && _entityManager.Exists(_servingEntity))
            {
                return _servingEntity;
            }
            return Entity.Null;
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
            DisposeServingResources();
        }

        private void DisposeServingResources()
        {
            if (_colliderBlobRef.IsCreated)
            {
                _colliderBlobRef.Dispose();
            }

            if (World.DefaultGameObjectInjectionWorld == null) return;
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (em.Exists(_servingEntity))
            {
                em.DestroyEntity(_servingEntity);
                _servingEntity = Entity.Null;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position + new Vector3(0, 0.5f, 0), colliderSize);

            if (queueStartPoint != null)
            {
                Gizmos.color = Color.green;
                Vector3 startPos = queueStartPoint.position;
                Gizmos.DrawSphere(startPos, 0.2f);

                // 줄 모양
                for (int i = 1; i < maxQueueSize; i++)
                {
                    Vector3 nextPos = startPos + (transform.forward * i * 1.0f);
                    Gizmos.DrawWireSphere(nextPos, 0.2f);
                }
            }
        }
    }
}