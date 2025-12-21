using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Authoring
{
    public class TrashCanAuthoring : MonoBehaviour
    {
        [Header("스테이션 설정")]
        public int stationID = 0;

        [Header("물리 설정")]
        public Vector3 colliderSize = new Vector3(1.0f, 1.0f, 1.0f);

        private Entity _trashCanEntity;
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
            _trashCanEntity = _entityManager.CreateEntity();
#if UNITY_EDITOR
            _entityManager.SetName(_trashCanEntity, $"TrashCan_{stationID}");
#endif
        }

        private void SetupTransform()
        {
            var position = transform.position;
            _entityManager.AddComponentData(_trashCanEntity, LocalTransform.FromPosition(position));
            _entityManager.AddComponentData(_trashCanEntity, new LocalToWorld
            {
                Value = float4x4.TRS(position, quaternion.identity, new float3(1))
            });
        }

        private void SetupGameLogicComponents()
        {
            _entityManager.AddComponentData(_trashCanEntity, new StationComponent
            {
                Type = StationType.TrashCan,
                StationID = stationID,
                PlacedItemEntity = Entity.Null
            });

            _entityManager.AddComponentData(_trashCanEntity, new InteractableComponent { IsActive = true });

            _entityManager.AddComponentData(_trashCanEntity, new TrashCanComponent());
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

            // [수정 포인트 2] 멤버 변수에 할당하여 관리 시작
            _colliderBlobRef = Unity.Physics.BoxCollider.Create(
                boxGeometry,
                new CollisionFilter
                {
                    BelongsTo = 1u << 6, // Interactable Layer
                    CollidesWith = ~0u,
                    GroupIndex = 0
                }
            );

            // 컴포넌트에는 참조값 전달
            _entityManager.AddComponentData(_trashCanEntity, new PhysicsCollider { Value = _colliderBlobRef });

            _entityManager.AddComponentData(_trashCanEntity, new PhysicsVelocity());
            _entityManager.AddComponentData(_trashCanEntity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
            _entityManager.AddComponent<Simulate>(_trashCanEntity);
            _entityManager.AddSharedComponent(_trashCanEntity, new PhysicsWorldIndex(0));
        }

        private void LateUpdate()
        {
            if (_entityManager.Exists(_trashCanEntity))
            {
                var lt = _entityManager.GetComponentData<LocalTransform>(_trashCanEntity);
                transform.position = lt.Position;
            }
        }

        private void OnDestroy()
        {
            DisposeTrashCanResources();
        }

        private void DisposeTrashCanResources()
        {
            if (_colliderBlobRef.IsCreated)
            {
                _colliderBlobRef.Dispose();
            }

            if (World.DefaultGameObjectInjectionWorld == null) return;
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (em.Exists(_trashCanEntity))
            {
                em.DestroyEntity(_trashCanEntity);
                _trashCanEntity = Entity.Null;
            }
        }



        private void OnDrawGizmosSelected()
        {
            if (this == null || gameObject == null) return;
            Gizmos.color = Color.gray; 
            if (transform != null)
                Gizmos.DrawWireCube(transform.position + new Vector3(0, 0, 0), colliderSize);
        }
    }
}