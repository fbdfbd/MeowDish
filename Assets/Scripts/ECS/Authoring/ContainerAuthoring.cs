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

        [Header("물리 설정")]
        public Vector3 colliderSize = new Vector3(1.5f, 2f, 1.5f);

        private Entity _containerEntity;
        private EntityManager _entityManager;

        private BlobAssetReference<Unity.Physics.Collider> _colliderBlobRef;


        private void Start()
        {
            if (!InitializeEntityManager()) return;

            CreateContainerEntity();

            SetupTransform();          
            SetupGameLogicComponents(); 
            SetupPhysics();           
        }

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
                IsActive = true
            });
        }

        private void SetupPhysics()
        {
            var boxGeometry = new BoxGeometry
            {
                Center = float3.zero,
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


            _entityManager.AddComponentData(_containerEntity, new PhysicsCollider { Value = _colliderBlobRef });

            _entityManager.AddComponentData(_containerEntity, new PhysicsVelocity()); // 정적.. 필요?
            _entityManager.AddComponentData(_containerEntity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
            _entityManager.AddComponent<Simulate>(_containerEntity);
            _entityManager.AddSharedComponent(_containerEntity, new PhysicsWorldIndex(0));
        }



        private void LateUpdate()
        {
            // 엔티티 위치 게임오브젝트 동기화
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
            if (_colliderBlobRef.IsCreated)
            {
                _colliderBlobRef.Dispose();
            }

            if (World.DefaultGameObjectInjectionWorld == null || !World.DefaultGameObjectInjectionWorld.IsCreated)
                return;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (em.Exists(_containerEntity))
            {
                em.DestroyEntity(_containerEntity);
                _containerEntity = Entity.Null;
            }
        }




        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, colliderSize);
        }
    }
}