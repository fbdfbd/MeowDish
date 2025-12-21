using Unity.Entities;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.View
{
    public class PlayerAnimationView : MonoBehaviour
    {
        private Animator _animator;
        private EntityManager _entityManager;
        private Entity _targetEntity;
        private bool _isInitialized = false;

        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int VictoryHash = Animator.StringToHash("Victory");
        private static readonly int FailHash = Animator.StringToHash("Fail");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
        }

        public void Initialize(Entity entity, EntityManager em)
        {
            _targetEntity = entity;
            _entityManager = em;
            _isInitialized = true;
        }

        // ------------------------------------------------
        private void LateUpdate()
        {
            if (!_isInitialized || !_entityManager.Exists(_targetEntity)) return;

            // IsMoving µø±‚»≠
            if (_entityManager.HasComponent<PlayerAnimationComponent>(_targetEntity))
            {
                var animState = _entityManager.GetComponentData<PlayerAnimationComponent>(_targetEntity);
                _animator.SetBool(IsMovingHash, animState.IsMoving);
            }
        }
    }
}