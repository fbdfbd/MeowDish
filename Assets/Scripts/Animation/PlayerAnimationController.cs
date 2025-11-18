using Unity.Entities;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.Animation
{
    /// <summary>
    /// 애니메이션 컨트롤러 (간소화 버전)
    /// 
    /// Idle ↔ Walk만 사용
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("References")]
        public Animator animator;

        // Animator Parameters (간단!)
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        
        // 이벤트 애니메이션
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int VictoryHash = Animator.StringToHash("Victory");
        private static readonly int FailHash = Animator.StringToHash("Fail");

        // ECS
        private Entity _playerEntity;
        private EntityManager _entityManager;
        private bool _isInitialized;

        private void Start()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (animator == null)
            {
                Debug.LogError("[PlayerAnimationController] Animator not found!");
                enabled = false;
            }
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                InitializeECS();
                if (!_isInitialized) return;
            }

            if (!_entityManager.Exists(_playerEntity))
            {
                _isInitialized = false;
                return;
            }

            var animState = _entityManager.GetComponentData<PlayerAnimationComponent>(_playerEntity);
            animator.SetBool(IsMovingHash, animState.IsMoving);
        }

        // ????????????????????????????????????????
        // 이벤트 애니메이션
        // ????????????????????????????????????????

        public void PlayDayClearAnimation()
        {
            animator.SetTrigger(JumpHash);
            Debug.Log("[PlayerAnimationController] Day Clear!");
        }

        public void PlayVictoryAnimation()
        {
            animator.SetTrigger(VictoryHash);
        }

        public void PlayFailAnimation()
        {
            animator.SetTrigger(FailHash);
        }

        // ????????????????????????????????????????
        // ECS 초기화
        // ????????????????????????????????????????
        private void InitializeECS()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogWarning("[PlayerAnimationController] World not found!");
                return;
            }

            _entityManager = world.EntityManager;

            var query = _entityManager.CreateEntityQuery(
                typeof(PlayerAnimationComponent),
                typeof(PlayerInputComponent)
            );

            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            if (entities.Length > 0)
            {
                _playerEntity = entities[0];
                _isInitialized = true;
                Debug.Log("[PlayerAnimationController] Connected to Entity!");
            }
            entities.Dispose();
        }
    }
}