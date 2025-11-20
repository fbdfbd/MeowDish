using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Authoring
{
    public class PlayerAuthoring : MonoBehaviour
    {
        [Header("기본 스탯")]
        public float BaseMoveSpeed = 5.0f;
        public float BaseActionSpeed = 1.0f;

        [Header("회전 설정")]
        [Tooltip("캐릭터 회전 속도 (값이 클수록 빠르게 회전)")]
        [Range(5f, 20f)]
        public float RotationSpeed = 10f;

        [Header("캐릭터 모델 설정")]
        [Tooltip("캐릭터 모델의 Forward 방향 보정 (Y축 회전)")]
        [Range(-180f, 180f)]
        public float ModelRotationOffset = 0f;

        [Header("플레이어 ID")]
        [Tooltip("멀티플레이어용 ID (싱글은 0)")]
        public int PlayerId = 0;

        private Entity _playerEntity;
        private EntityManager _entityManager;
        private float _rotationOffsetRadians;

        private void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _rotationOffsetRadians = math.radians(ModelRotationOffset);

            _playerEntity = _entityManager.CreateEntity();

            var position = transform.position;
            // ? LocalTransform 추가
            _entityManager.AddComponentData(_playerEntity,
                LocalTransform.FromPosition(position));

            // ? LocalToWorld 추가 (중요!)
            _entityManager.AddComponentData(_playerEntity, new LocalToWorld
            {
                Value = float4x4.TRS(position, quaternion.identity, new float3(1, 1, 1))
            });
            _entityManager.AddComponent<Simulate>(_playerEntity);
            Debug.Log("[PlayerAuthoring] ? Simulate 태그 추가!");
            // Input
            _entityManager.AddComponentData(_playerEntity, new PlayerInputComponent
            {
                MoveInput = float2.zero,
                InteractTapped = false,
                InteractHoldStarted = false,
                InteractHolding = false
            });

            // PlayerStateComponent
            _entityManager.AddComponentData(_playerEntity, new PlayerStateComponent
            {
                PlayerId = PlayerId,
                HeldItemEntity = Entity.Null,
                IsHoldingItem = false,
                CurrentStationEntity = Entity.Null,
                IsNearStation = false
            });

            // Stats
            _entityManager.AddComponentData(_playerEntity, new PlayerStatsComponent
            {
                BaseMoveSpeed = BaseMoveSpeed,
                BaseActionSpeed = BaseActionSpeed,
                RotationSpeed = RotationSpeed,
                MoveSpeedBonus = 0,
                ActionSpeedBonus = 0,
                MoveSpeedMultiplier = 1.0f,
                ActionSpeedMultiplier = 1.0f,
                AllSpeedMultiplier = 1.0f
            });

            // Animation
            _entityManager.AddComponentData(_playerEntity, new PlayerAnimationComponent
            {
                IsMoving = false
            });

            // Equipment
            _entityManager.AddComponentData(_playerEntity, new EquipmentSlots());

            // Buffers
            _entityManager.AddBuffer<PermanentUpgrade>(_playerEntity);
            _entityManager.AddBuffer<TemporaryBuff>(_playerEntity);

            // ========================================
            // ? Physics 컴포넌트
            // ========================================

            // PhysicsCollider
            // PhysicsCollider
            var playerCollider = Unity.Physics.SphereCollider.Create(
                new Unity.Physics.SphereGeometry
                {
                    Center = float3.zero,
                    Radius = 0.5f
                },
                new Unity.Physics.CollisionFilter
                {
                    BelongsTo = 1u << 0,      // Layer 0 (Player)
                    CollidesWith = ~0u,       // ? 모든 레이어와 충돌 (원래: 1u << 1)
                    GroupIndex = 0
                }
            );


            _entityManager.AddComponentData(_playerEntity, new Unity.Physics.PhysicsCollider
            {
                Value = playerCollider
            });

            // PhysicsVelocity
            _entityManager.AddComponentData(_playerEntity, new Unity.Physics.PhysicsVelocity
            {
                Linear = float3.zero,
                Angular = float3.zero
            });

            // ? PhysicsMass - Kinematic Body (타이쿤 게임에 최적)
            _entityManager.AddComponentData(_playerEntity, Unity.Physics.PhysicsMass.CreateKinematic(
                Unity.Physics.MassProperties.UnitSphere
            ));

            // PhysicsDamping - 관성 제거
            _entityManager.AddComponentData(_playerEntity, new Unity.Physics.PhysicsDamping
            {
                Linear = 10f,
                Angular = 10f
            });

            // 중력 끄기
            _entityManager.AddComponentData(_playerEntity, new Unity.Physics.PhysicsGravityFactor
            {
                Value = 0f
            });

            Debug.Log("[PlayerAuthoring] ? Entity created with Kinematic Physics Body!");
        }

        private void LateUpdate()
        {
            if (_entityManager.Exists(_playerEntity))
            {
                var lt = _entityManager.GetComponentData<LocalTransform>(_playerEntity);
                transform.position = lt.Position;

                // 회전 오프셋 적용
                quaternion offsetRotation = quaternion.RotateY(_rotationOffsetRadians);
                transform.rotation = math.mul(lt.Rotation, offsetRotation);

                // 디버그: 플레이어 상태 표시 (옵션)
#if UNITY_EDITOR
                var playerState = _entityManager.GetComponentData<PlayerStateComponent>(_playerEntity);
                if (playerState.IsNearStation)
                {
                    Debug.DrawLine(transform.position, transform.position + Vector3.up * 2f, Color.yellow);
                }
#endif
            }
        }

        // 1. OnDisable 메서드 추가 (OnDestroy 위에)
        private void OnDisable()
        {
            // Play Mode 종료 시 메모리 누수 방지를 위해 OnDisable에서 먼저 정리
            DisposePlayerResources();
        }

        // 2. OnDestroy를 DisposePlayerResources 호출로 변경
        private void OnDestroy()
        {
            // OnDisable에서 이미 정리했지만, 혹시 모를 경우를 대비해 다시 호출
            DisposePlayerResources();
        }

        // 3. 실제 정리 로직을 별도 메서드로 분리
        private void DisposePlayerResources()
        {
            if (World.DefaultGameObjectInjectionWorld == null ||
                !World.DefaultGameObjectInjectionWorld.IsCreated)
                return;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (em.Exists(_playerEntity))
            {
                // Collider Dispose (메모리 누수 방지)
                if (em.HasComponent<Unity.Physics.PhysicsCollider>(_playerEntity))
                {
                    var collider = em.GetComponentData<Unity.Physics.PhysicsCollider>(_playerEntity);
                    if (collider.Value.IsCreated)
                    {
                        collider.Value.Dispose();
                    }
                }

                em.DestroyEntity(_playerEntity);
                _playerEntity = Entity.Null;  // 중복 Dispose 방지
            }
        }

        // 기즈모: 플레이어 상태 시각화
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && _entityManager != null && _entityManager.Exists(_playerEntity))
            {
                var playerState = _entityManager.GetComponentData<PlayerStateComponent>(_playerEntity);

                // 플레이어 위치
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, 0.5f);

                // 들고 있는 아이템 표시
                if (playerState.IsHoldingItem)
                {
                    Gizmos.color = Color.green;
                    Vector3 itemPos = transform.position + Vector3.up * 1f + transform.forward * 0.5f;
                    Gizmos.DrawCube(itemPos, Vector3.one * 0.3f);
                }

                // 근처 스테이션 연결선
                if (playerState.IsNearStation && _entityManager.Exists(playerState.CurrentStationEntity))
                {
                    var stationTransform = _entityManager.GetComponentData<LocalTransform>(playerState.CurrentStationEntity);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(transform.position, stationTransform.Position);
                }

#if UNITY_EDITOR
                // 상태 텍스트
                Vector3 labelPos = transform.position + Vector3.up * 2.5f;
                string info = $"Player {PlayerId}";

                if (playerState.IsHoldingItem)
                    info += "\n?? Holding Item";
                if (playerState.IsNearStation)
                    info += "\n? Near Station";

                UnityEditor.Handles.Label(labelPos, info, new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = new GUIStyleState() { textColor = Color.cyan },
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                });
#endif
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 선택했을 때 추가 정보
            if (Application.isPlaying && _entityManager != null && _entityManager.Exists(_playerEntity))
            {
                var stats = _entityManager.GetComponentData<PlayerStatsComponent>(_playerEntity);

                // 이동속도 시각화
                Gizmos.color = new Color(0, 1, 0, 0.3f);
            }
        }
    }
}