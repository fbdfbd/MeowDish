using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;
using Meow.ECS.Components;
using System.Collections.Generic; // List 사용을 위해 추가

namespace Meow.ECS.Authoring
{
    public class CounterAuthoring : MonoBehaviour
    {
        // =========================================================
        // 1. 인스펙터 설정
        // =========================================================
        [Header("카운터 설정")]
        [Tooltip("초기 슬롯 개수")]
        public int initialSlotCount = 1;

        [Header("슬롯 위치 설정 (Visual)")]
        [Tooltip("아이템이 놓일 위치들 (빈 GameObject를 만들어 여기에 드래그하세요)")]
        public List<Transform> snapPoints; // ?? 여기가 핵심! 4구면 4개 넣으세요.

        [Header("스테이션 설정")]
        public int stationID = 0;

        [Header("물리 설정")]
        public Vector3 colliderSize = new Vector3(1.5f, 1.0f, 1.5f);

        // =========================================================
        // 2. 내부 변수
        // =========================================================
        private Entity _counterEntity;
        private EntityManager _entityManager;

        // =========================================================
        // 3. 초기화 (목차 스타일)
        // =========================================================
        private void Start()
        {
            InitializeEntityManager();
            
            // ID 자동 생성 (안 적었을 경우 대비)
            if (stationID == 0) stationID = gameObject.GetInstanceID();

            CreateCounterEntity();

            SetupTransform();
            SetupGameLogicComponents(); // ?? 여기에 위치 등록 로직이 숨어있음
            SetupPhysics();
        }

        // =========================================================
        // 4. 세부 설정 메서드들
        // =========================================================

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
            // 1. Station Component
            _entityManager.AddComponentData(_counterEntity, new StationComponent
            {
                Type = StationType.Counter, // Enum에 Counter가 없다면 WorkBench 등 사용
                StationID = stationID,
                PlacedItemEntity = Entity.Null
            });

            // 2. Interactable (상호작용 가능)
            _entityManager.AddComponentData(_counterEntity, new InteractableComponent 
            { 
                IsActive = true 
            });

            // 3. Counter Component (설정)
            _entityManager.AddComponentData(_counterEntity, new CounterComponent
            {
                MaxItems = initialSlotCount
            });

            // 4. 아이템 슬롯 버퍼 (논리적 저장소)
            _entityManager.AddBuffer<CounterItemSlot>(_counterEntity);

            // 5. ?? [핵심] 위치 정보 버퍼 (시각적 위치)
            var snapBuffer = _entityManager.AddBuffer<CounterSnapPoint>(_counterEntity);
            
            if (snapPoints != null && snapPoints.Count > 0)
            {
                foreach (var point in snapPoints)
                {
                    if (point != null)
                    {
                        // 부모(카운터) 기준 로컬 위치를 저장해야 함!
                        snapBuffer.Add(new CounterSnapPoint
                        {
                            LocalPosition = point.localPosition
                        });
                    }
                }
            }
            else
            {
                // 설정 안 했으면 기본값 (중앙 위) 하나 추가
                snapBuffer.Add(new CounterSnapPoint { LocalPosition = new float3(0, 1.1f, 0) });
                Debug.LogWarning($"[{name}] SnapPoint가 설정되지 않았습니다. 기본값(중앙)을 사용합니다.");
            }
        }

        private void SetupPhysics()
        {
            var boxGeometry = new BoxGeometry
            {
                Center = new float3(0, 0f, 0), // 바닥 기준 높이 보정 (필요시 수정)
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

            _entityManager.AddComponentData(_counterEntity, new PhysicsCollider { Value = collider });
            _entityManager.AddComponentData(_counterEntity, new PhysicsVelocity()); // 정적이지만 구조상 추가
            _entityManager.AddComponentData(_counterEntity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
            _entityManager.AddComponent<Simulate>(_counterEntity);
            _entityManager.AddSharedComponent(_counterEntity, new PhysicsWorldIndex(0));
        }

        // =========================================================
        // 5. 업데이트 및 해제
        // =========================================================
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
            if (World.DefaultGameObjectInjectionWorld == null) return;
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (em.Exists(_counterEntity))
            {
                if (em.HasComponent<PhysicsCollider>(_counterEntity))
                {
                    var col = em.GetComponentData<PhysicsCollider>(_counterEntity);
                    if (col.Value.IsCreated) col.Value.Dispose();
                }
                em.DestroyEntity(_counterEntity);
            }
        }

        // =========================================================
        // 6. 디버그 (Gizmos)
        // =========================================================
        private void OnDrawGizmos()
        {
            // 콜라이더 박스
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + new Vector3(0, 0.5f, 0), colliderSize);

            // ?? 슬롯 위치 미리보기 (중요)
            if (snapPoints != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var point in snapPoints)
                {
                    if (point != null)
                    {
                        // 실제 게임 뷰에서의 위치 표시
                        Gizmos.DrawWireSphere(point.position, 0.15f);
                    }
                }
            }
        }
    }
}