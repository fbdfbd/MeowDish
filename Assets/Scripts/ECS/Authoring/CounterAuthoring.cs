using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;
using Meow.ECS.Components;
using System.Collections.Generic;

namespace Meow.ECS.Authoring
{
    public class CounterAuthoring : MonoBehaviour
    {
        [Header("카운터 설정")]
        [Tooltip("초기 슬롯 개수")]
        public int initialSlotCount = 1;

        [Header("슬롯 위치 설정 (Visual)")]
        public List<Transform> snapPoints;

        [Header("스테이션 설정")]
        public int stationID = 0;

        [Header("물리 설정")]
        public Vector3 colliderSize = new Vector3(1.5f, 1.0f, 1.5f);

        private Entity _counterEntity;
        private EntityManager _entityManager;

        private BlobAssetReference<Unity.Physics.Collider> _colliderBlobRef;


        private void Start()
        {
            InitializeEntityManager();

            if (stationID == 0) stationID = gameObject.GetInstanceID();

            CreateCounterEntity();

            SetupTransform();
            SetupGameLogicComponents();
            SetupPhysics();
        }

        private void InitializeEntityManager()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void CreateCounterEntity()
        {
            _counterEntity = _entityManager.CreateEntity();
#if UNITY_EDITOR
            _entityManager.SetName(_counterEntity, $"Counter_{stationID}");
#endif
        }

        private void SetupTransform()
        {
            var position = transform.position;
            _entityManager.AddComponentData(_counterEntity, LocalTransform.FromPosition(position));
            _entityManager.AddComponentData(_counterEntity, new LocalToWorld
            {
                Value = float4x4.TRS(position, quaternion.identity, new float3(1))
            });
        }

        private void SetupGameLogicComponents()
        {
            _entityManager.AddComponentData(_counterEntity, new StationComponent
            {
                Type = StationType.Counter,
                StationID = stationID,
                PlacedItemEntity = Entity.Null
            });

            _entityManager.AddComponentData(_counterEntity, new InteractableComponent
            {
                IsActive = true
            });

            _entityManager.AddComponentData(_counterEntity, new CounterComponent
            {
                MaxItems = initialSlotCount
            });

            // 아이템 슬롯 버퍼
            _entityManager.AddBuffer<CounterItemSlot>(_counterEntity);

            var snapBuffer = _entityManager.AddBuffer<CounterSnapPoint>(_counterEntity);

            if (snapPoints != null && snapPoints.Count > 0)
            {
                foreach (var point in snapPoints)
                {
                    if (point != null)
                    {
                        snapBuffer.Add(new CounterSnapPoint
                        {
                            LocalPosition = point.localPosition
                        });
                    }
                }
            }
            else
            {
                snapBuffer.Add(new CounterSnapPoint { LocalPosition = new float3(0, 1.1f, 0) });
                Debug.LogWarning($"[{name}] SnapPoint 기본값 사용");
            }
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
                    BelongsTo = 1u << 6, // Interactable Layer
                    CollidesWith = ~0u,
                    GroupIndex = 0
                }
            );

            _entityManager.AddComponentData(_counterEntity, new PhysicsCollider { Value = _colliderBlobRef });

            _entityManager.AddComponentData(_counterEntity, new PhysicsVelocity());
            _entityManager.AddComponentData(_counterEntity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
            _entityManager.AddComponent<Simulate>(_counterEntity);
            _entityManager.AddSharedComponent(_counterEntity, new PhysicsWorldIndex(0));
        }


        private void LateUpdate()
        {
            if (_entityManager.Exists(_counterEntity))
            {
                var lt = _entityManager.GetComponentData<LocalTransform>(_counterEntity);
                transform.position = lt.Position;
            }
        }

        private void OnDestroy()
        {
            DisposeCounterResources();
        }

        private void DisposeCounterResources()
        {
            if (_colliderBlobRef.IsCreated)
            {
                _colliderBlobRef.Dispose();
            }

            if (World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.IsCreated)
            {
                var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                if (em.Exists(_counterEntity))
                {
                    em.DestroyEntity(_counterEntity);
                    _counterEntity = Entity.Null;
                }
            }
        }




        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + new Vector3(0, 0, 0), colliderSize);

            // 슬롯 위치 미리보기
            if (snapPoints != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var point in snapPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 0.15f);
                    }
                }
            }
        }
    }
}