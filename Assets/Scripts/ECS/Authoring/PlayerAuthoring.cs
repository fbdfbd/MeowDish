using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;
using Meow.ECS.Components;
using Meow.ECS.View;
using Meow.Data;

namespace Meow.ECS.Authoring
{
    public class PlayerAuthoring : MonoBehaviour
    {
        // =========================================================
        // 1. 인스펙터 설정
        // =========================================================
        [Header("데이터 에셋 연결")]
        [Tooltip("플레이어 스탯이 담긴 ScriptableObject")]
        public PlayerStatsSO StatsData;

        [Header("개별 설정")]
        [Tooltip("캐릭터 모델의 Forward 방향 보정 (Y축 회전)")]
        [Range(-180f, 180f)]
        public float ModelRotationOffset = 0f;

        [Header("플레이어 ID")]
        public int PlayerId = 0;

        [Header("물리 설정")]
        [Tooltip("플레이어 충돌 반경 (기본 0.5 -> 뚱뚱하면 0.3으로 줄이세요)")]
        public float playerRadius = 0.5f; // ?? 여기서 조절 가능!

        // =========================================================
        // 2. 내부 변수
        // =========================================================
        private Entity _playerEntity;
        private EntityManager _entityManager;
        private float _rotationOffsetRadians;
        private BlobAssetReference<PlayerBaseStats> _statsBlobRef;

        // =========================================================
        // 3. 초기화 (목차)
        // =========================================================
        private void Start()
        {
            // 안전장치
            if (StatsData == null)
            {
                Debug.LogError($"[{name}] PlayerStatsSO가 연결되지 않았습니다! StatsData에 에셋을 넣어주세요.");
                return;
            }

            InitializeEntityManager();
            CreatePlayerEntity();

            SetupTransform();
            SetupStatsAndConfig();
            SetupGameLogicComponents();
            SetupPhysics();

            ConnectToView();
        }

        // =========================================================
        // 4. 세부 설정 메서드들
        // =========================================================

        private void InitializeEntityManager()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _rotationOffsetRadians = math.radians(ModelRotationOffset);
        }

        private void CreatePlayerEntity()
        {
            _playerEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(_playerEntity, new PlayerTag { });

#if UNITY_EDITOR
            _entityManager.SetName(_playerEntity, $"Player_{PlayerId}");
#endif
        }

        private void SetupTransform()
        {
            var position = transform.position;
            _entityManager.AddComponentData(_playerEntity, LocalTransform.FromPosition(position));
            _entityManager.AddComponentData(_playerEntity, new LocalToWorld
            {
                Value = float4x4.TRS(position, quaternion.identity, new float3(1, 1, 1))
            });
            _entityManager.AddComponent<Simulate>(_playerEntity);
        }

        private void SetupStatsAndConfig()
        {
            // 1. 불변 데이터 (Blob) 생성 - SO에서 값 읽어오기
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref PlayerBaseStats rootStats = ref builder.ConstructRoot<PlayerBaseStats>();

                // SO의 데이터를 Blob에 주입
                rootStats.BaseMoveSpeed = StatsData.BaseMoveSpeed;
                rootStats.BaseActionSpeed = StatsData.BaseActionSpeed;
                rootStats.RotationSpeed = StatsData.RotationSpeed;

                _statsBlobRef = builder.CreateBlobAssetReference<PlayerBaseStats>(Allocator.Persistent);
            }

            // 2. Config 연결
            _entityManager.AddComponentData(_playerEntity, new UnitConfig
            {
                BlobRef = _statsBlobRef
            });

            // 3. 가변 데이터 (Stats) 생성
            _entityManager.AddComponentData(_playerEntity, new PlayerStatsComponent
            {
                MoveSpeedBonus = 0f,
                ActionSpeedBonus = 0f,
                MoveSpeedMultiplier = 1.0f,
                ActionSpeedMultiplier = 1.0f,
                AllSpeedMultiplier = 1.0f
            });
        }

        private void SetupGameLogicComponents()
        {
            // Input
            _entityManager.AddComponentData(_playerEntity, new PlayerInputComponent());

            // State
            _entityManager.AddComponentData(_playerEntity, new PlayerStateComponent
            {
                PlayerId = PlayerId,
                HeldItemEntity = Entity.Null,
                CurrentStationEntity = Entity.Null,
                LastMoveDirection = math.forward()
            });

            // Animation (데이터)
            _entityManager.AddComponentData(_playerEntity, new PlayerAnimationComponent());
        }

        private void ConnectToView()
        {
            var animView = GetComponent<PlayerAnimationView>();
            if (animView != null)
            {
                animView.Initialize(_playerEntity, _entityManager);
            }
            else
            {
                Debug.LogWarning($"[{name}] PlayerAnimationView가 없습니다! 애니메이션이 작동하지 않습니다.");
            }
        }

        private void SetupPhysics()
        {
            // ?? [핵심 수정] playerRadius 변수를 사용해서 콜라이더 생성
            var playerCollider = Unity.Physics.SphereCollider.Create(
                new Unity.Physics.SphereGeometry
                {
                    Center = new float3(0,0,0),
                    Radius = playerRadius // <--- 변수 사용!
                },
                new Unity.Physics.CollisionFilter
                {
                    BelongsTo = 1u << 0,
                    CollidesWith = ~0u,
                    GroupIndex = 0
                }
            );

            _entityManager.AddComponentData(_playerEntity, new PhysicsCollider { Value = playerCollider });
            _entityManager.AddComponentData(_playerEntity, new PhysicsVelocity());
            _entityManager.AddComponentData(_playerEntity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
            _entityManager.AddComponentData(_playerEntity, new PhysicsDamping { Linear = 10f, Angular = 10f });
            _entityManager.AddComponentData(_playerEntity, new PhysicsGravityFactor { Value = 0f });
        }

        // =========================================================
        // 5. 업데이트 및 해제
        // =========================================================

        private void LateUpdate()
        {
            // 엔티티의 위치/회전을 GameObject에 동기화
            if (_entityManager.Exists(_playerEntity))
            {
                var lt = _entityManager.GetComponentData<LocalTransform>(_playerEntity);
                transform.position = lt.Position;

                quaternion offsetRotation = quaternion.RotateY(_rotationOffsetRadians);
                transform.rotation = math.mul(lt.Rotation, offsetRotation);
            }
        }

        private void OnDisable() => DisposePlayerResources();
        private void OnDestroy() => DisposePlayerResources();

        private void DisposePlayerResources()
        {
            if (World.DefaultGameObjectInjectionWorld == null || !World.DefaultGameObjectInjectionWorld.IsCreated) return;
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            // Blob 수동 해제
            if (_statsBlobRef.IsCreated) _statsBlobRef.Dispose();

            // 엔티티 및 Collider 해제
            if (em.Exists(_playerEntity))
            {
                if (em.HasComponent<PhysicsCollider>(_playerEntity))
                {
                    var collider = em.GetComponentData<PhysicsCollider>(_playerEntity);
                    if (collider.Value.IsCreated) collider.Value.Dispose();
                }
                em.DestroyEntity(_playerEntity);
                _playerEntity = Entity.Null;
            }
        }

        // =========================================================
        // 6. 디버그 (Gizmos)
        // =========================================================
        private void OnDrawGizmos()
        {
            if (this == null || gameObject == null) return;

            // 실행 중이고 엔티티가 살아있을 때
            if (Application.isPlaying && _entityManager != null && _entityManager.Exists(_playerEntity))
            {
                var playerState = _entityManager.GetComponentData<PlayerStateComponent>(_playerEntity);

                // 1. 플레이어 위치
                Gizmos.color = Color.blue;
                // ?? 기즈모도 실제 반경으로 그리기
                Gizmos.DrawWireSphere(transform.position, playerRadius);

                // 2. 들고 있는 아이템 표시
                if (playerState.IsHoldingItem)
                {
                    Gizmos.color = Color.green;
                    Vector3 itemPos = transform.position + Vector3.up * 1f + transform.forward * 0.5f;
                    Gizmos.DrawCube(itemPos, Vector3.one * 0.3f);
                }

                // 3. 근처 스테이션 연결선
                if (playerState.IsNearStation && _entityManager.Exists(playerState.CurrentStationEntity))
                {
                    var stationTransform = _entityManager.GetComponentData<LocalTransform>(playerState.CurrentStationEntity);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(transform.position, stationTransform.Position);
                }

#if UNITY_EDITOR
                // 4. 상태 텍스트
                Vector3 labelPos = transform.position + Vector3.up * 2.5f;
                string info = $"Player {PlayerId}";

                if (playerState.IsHoldingItem)
                    info += "\n?? Holding Item";
                if (playerState.IsNearStation)
                    info += "\n?? Near Station";

                UnityEditor.Handles.Label(labelPos, info, new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = new GUIStyleState() { textColor = Color.cyan },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                });
#endif
            }
            else
            {
                // 플레이 전 에디터 상태일 때
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, playerRadius);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 필요시 추가
        }
    }
}