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

            // Transform
            _entityManager.AddComponentData(_playerEntity,
                LocalTransform.FromPosition(transform.position));

            // Input
            _entityManager.AddComponentData(_playerEntity, new PlayerInputComponent
            {
                MoveInput = float2.zero,
                InteractPressed = false
            });

            // ????????????????????????????????????????
            // ?? PlayerStateComponent 추가!
            // ????????????????????????????????????????
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

            Debug.Log("[PlayerAuthoring] Entity created with all components!");
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

                // ????????????????????????????????????????
                // ?? 디버그: 플레이어 상태 표시 (옵션)
                // ????????????????????????????????????????
#if UNITY_EDITOR
                var playerState = _entityManager.GetComponentData<PlayerStateComponent>(_playerEntity);
                if (playerState.IsNearStation)
                {
                    Debug.DrawLine(transform.position, transform.position + Vector3.up * 2f, Color.yellow);
                }
#endif
            }
        }

        private void OnDestroy()
        {
            if (_entityManager != null && _entityManager.Exists(_playerEntity))
            {
                _entityManager.DestroyEntity(_playerEntity);
            }
        }

        // ????????????????????????????????????????
        // ?? 기즈모: 플레이어 상태 시각화
        // ????????????????????????????????????????
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
                    info += "\n?? Near Station";

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