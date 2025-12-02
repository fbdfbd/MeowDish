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

        // ------------------------------------------------
        // 1. 해시 최적화 (성능 굿)
        // ------------------------------------------------
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int VictoryHash = Animator.StringToHash("Victory");
        private static readonly int FailHash = Animator.StringToHash("Fail");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
        }

        // ------------------------------------------------
        // 2. 초기화 (Authoring에서 호출해줌 -> 안전함!)
        // ------------------------------------------------
        public void Initialize(Entity entity, EntityManager em)
        {
            _targetEntity = entity;
            _entityManager = em;
            _isInitialized = true;
        }

        // ------------------------------------------------
        // 3. 매 프레임 ECS 데이터 감시
        // ------------------------------------------------
        private void LateUpdate()
        {
            // 연결 안 됐거나 엔티티 죽었으면 중단
            if (!_isInitialized || !_entityManager.Exists(_targetEntity)) return;

            // IsMoving 동기화
            if (_entityManager.HasComponent<PlayerAnimationComponent>(_targetEntity))
            {
                var animState = _entityManager.GetComponentData<PlayerAnimationComponent>(_targetEntity);
                _animator.SetBool(IsMovingHash, animState.IsMoving);
            }
        }

        // ------------------------------------------------
        // 4. 외부 이벤트용 함수 (옛날 코드 기능 복구)
        // ------------------------------------------------

        public void PlayDayClear()
        {
            _animator.SetTrigger(JumpHash);
            Debug.Log("[AnimView] Day Clear! Jump!");
        }

        public void PlayVictory()
        {
            _animator.SetTrigger(VictoryHash);
        }

        public void PlayFail()
        {
            _animator.SetTrigger(FailHash);
        }
    }
}