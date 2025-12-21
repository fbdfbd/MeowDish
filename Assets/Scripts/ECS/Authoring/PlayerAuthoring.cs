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
        [Header("플레이어 스탯")]
        public PlayerStatsSO StatsData;

        [Header("개별 설정")]
        [Tooltip("캐릭터 모델의 Forward 방향 보정 (Y축 회전)")]
        [Range(-180f, 180f)]
        public float ModelRotationOffset = 0f;

        [Header("플레이어 ID")]
        public int PlayerId = 0;

        [Header("물리 설정")]
        public float playerRadius = 0.5f;


        private Entity _playerEntity;
        private EntityManager _entityManager;
        private float _rotationOffsetRadians;

        private BlobAssetReference<PlayerBaseStats> _statsBlobRef;
        private BlobAssetReference<Unity.Physics.Collider> _colliderBlobRef;


        private void Start()
        {
            // 안전장치
            if (StatsData == null)
            {
                Debug.LogError($"[{name}] PlayerStatsSO가 연결되지 않았습니다.");
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
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref PlayerBaseStats rootStats = ref builder.ConstructRoot<PlayerBaseStats>();

                // SO의 데이터 Blob 주입
                rootStats.BaseMoveSpeed = StatsData.BaseMoveSpeed;
                rootStats.BaseActionSpeed = StatsData.BaseActionSpeed;
                rootStats.RotationSpeed = StatsData.RotationSpeed;

                // 수동 해제 필요
                _statsBlobRef = builder.CreateBlobAssetReference<PlayerBaseStats>(Allocator.Persistent);
            }

            _entityManager.AddComponentData(_playerEntity, new UnitConfig
            {
                BlobRef = _statsBlobRef
            });

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
            _entityManager.AddComponentData(_playerEntity, new PlayerInputComponent());

            _entityManager.AddComponentData(_playerEntity, new PlayerStateComponent
            {
                PlayerId = PlayerId,
                HeldItemEntity = Entity.Null,
                CurrentStationEntity = Entity.Null,
                LastMoveDirection = math.forward()
            });

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
            _colliderBlobRef = Unity.Physics.SphereCollider.Create(
                new Unity.Physics.SphereGeometry
                {
                    Center = new float3(0, 0, 0),
                    Radius = playerRadius
                },
                new Unity.Physics.CollisionFilter
                {
                    BelongsTo = 1u << 0,
                    CollidesWith = ~0u,
                    GroupIndex = 0
                }
            );

            _entityManager.AddComponentData(_playerEntity, new PhysicsCollider { Value = _colliderBlobRef });

            _entityManager.AddComponentData(_playerEntity, new PhysicsVelocity());
            _entityManager.AddComponentData(_playerEntity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
            _entityManager.AddComponentData(_playerEntity, new PhysicsDamping { Linear = 10f, Angular = 10f });
            _entityManager.AddComponentData(_playerEntity, new PhysicsGravityFactor { Value = 0f });
        }

        private void LateUpdate()
        {
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
            if (_statsBlobRef.IsCreated)
                _statsBlobRef.Dispose();

            if (_colliderBlobRef.IsCreated)
                _colliderBlobRef.Dispose();

            if (World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.IsCreated)
            {
                var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                if (em.Exists(_playerEntity))
                {
                    em.DestroyEntity(_playerEntity);
                    _playerEntity = Entity.Null;
                }
            }
        }




        private void OnDrawGizmos()
        {
            if (this == null || gameObject == null) return;

            if (Application.isPlaying && _entityManager != null && _entityManager.Exists(_playerEntity))
            {
                var playerState = _entityManager.GetComponentData<PlayerStateComponent>(_playerEntity);

                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, playerRadius);

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
                // 태 텍스트
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
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, playerRadius);
            }
        }
    }
}