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
            // 1. Station Component
            _entityManager.AddComponentData(_trashCanEntity, new StationComponent
            {
                Type = StationType.TrashCan, // Enum에 TrashCan(8) 있어야 함!
                StationID = stationID,
                PlacedItemEntity = Entity.Null
            });

            // 2. Interactable
            _entityManager.AddComponentData(_trashCanEntity, new InteractableComponent { IsActive = true });

            // 3. TrashCan Component (빈 태그성 컴포넌트)
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

            var collider = Unity.Physics.BoxCollider.Create(
                boxGeometry,
                new CollisionFilter
                {
                    BelongsTo = 1u << 6, // Interactable Layer
                    CollidesWith = ~0u,
                    GroupIndex = 0
                }
            );

            _entityManager.AddComponentData(_trashCanEntity, new PhysicsCollider { Value = collider });
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
            if (World.DefaultGameObjectInjectionWorld == null) return;
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (em.Exists(_trashCanEntity))
            {
                if (em.HasComponent<PhysicsCollider>(_trashCanEntity))
                {
                    var col = em.GetComponentData<PhysicsCollider>(_trashCanEntity);
                    if (col.Value.IsCreated) col.Value.Dispose();
                }
                em.DestroyEntity(_trashCanEntity);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (this == null || gameObject == null) return;
            Gizmos.color = Color.gray; // 쓰레기통은 회색
            if (transform != null)
                Gizmos.DrawWireCube(transform.position + new Vector3(0, colliderSize.y * 0.5f, 0), colliderSize);
        }
    }
}