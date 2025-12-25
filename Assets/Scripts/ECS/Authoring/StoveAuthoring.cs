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
        [Header("스토브 설정")]
        public float cookingSpeed = 1.0f;

        [Header("시각적 위치 설정")]
        public Transform snapPoint;

        [Header("스테이션 설정")]
        public int stationID = 0;

        [Header("물리 설정")]
        public Vector3 colliderSize = new Vector3(1.5f, 1.0f, 1.5f); 

        private Entity _stoveEntity;
        private EntityManager _entityManager;

        private BlobAssetReference<Unity.Physics.Collider> _colliderBlobRef;

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
            _entityManager.AddComponentData(_stoveEntity, new StationComponent
            {
                Type = StationType.Stove,
                StationID = stationID,
                PlacedItemEntity = Entity.Null
            });

            _entityManager.AddComponentData(_stoveEntity, new InteractableComponent
            {
                IsActive = true
            });

            _entityManager.AddComponentData(_stoveEntity, new StoveComponent
            {
                CookingSpeedMultiplier = cookingSpeed
            });

            _entityManager.AddComponentData(_stoveEntity, new StoveCookingState
            {
                ItemEntity = Entity.Null,
                CurrentCookProgress = 0f,
                IsCooking = false
            });

            float3 snapLocalPos = new float3(0, 1.0f, 0);
            if (snapPoint != null) snapLocalPos = snapPoint.localPosition;

            _entityManager.AddComponentData(_stoveEntity, new StoveSnapPoint
            {
                LocalPosition = snapLocalPos
            });
        }

        private void SetupPhysics()
        {
            var boxGeometry = new BoxGeometry
            {
                Center = new float3(0, 0f, 0),
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

            _entityManager.AddComponentData(_stoveEntity, new PhysicsCollider { Value = _colliderBlobRef });

            _entityManager.AddComponentData(_stoveEntity, new PhysicsVelocity());
            _entityManager.AddComponentData(_stoveEntity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
            _entityManager.AddComponent<Simulate>(_stoveEntity); 
            _entityManager.AddSharedComponent(_stoveEntity, new PhysicsWorldIndex(0));
        }

        public Entity GetEntity()
        {
            if (_entityManager != default && _entityManager.Exists(_stoveEntity))
            {
                return _stoveEntity;
            }
            return Entity.Null;
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
            DisposeStoveResources();
        }

        private void DisposeStoveResources()
        {
            if (_colliderBlobRef.IsCreated)
            {
                _colliderBlobRef.Dispose();
            }

            if (World.DefaultGameObjectInjectionWorld == null) return;
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (em.Exists(_stoveEntity))
            {
                em.DestroyEntity(_stoveEntity);
                _stoveEntity = Entity.Null;
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