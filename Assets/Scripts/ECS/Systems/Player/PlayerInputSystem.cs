using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class PlayerInputSystem : SystemBase
    {
        private @InputSystem_Actions _inputActions;

        // ?? [추가] "이번 클릭에서 홀드가 발동했었나?"를 기억하는 변수
        private bool _holdTriggeredThisPress = false;

        protected override void OnCreate()
        {
            RequireForUpdate<PlayerInputComponent>();
            _inputActions = new @InputSystem_Actions();
            _inputActions.Enable();
        }

        protected override void OnDestroy()
        {
            _inputActions.Disable();
            _inputActions.Dispose();
        }

        protected override void OnUpdate()
        {
            // 1. 이동 입력
            Vector2 moveVec = _inputActions.Player.Move.ReadValue<Vector2>();
            float2 input = new float2(moveVec.x, moveVec.y);

            // 2. 상호작용 입력
            bool interactTapped = false;
            bool interactHoldStarted = false;
            bool interactHolding = false;

            var interactAction = _inputActions.Player.Interact;

            // A. 버튼을 누르기 시작했을 때 -> 기억 리셋
            if (interactAction.WasPressedThisFrame())
            {
                _holdTriggeredThisPress = false;
            }

            // B. 누르고 있는 상태
            interactHolding = interactAction.IsPressed();

            // C. 홀드 시간(0.5초) 달성! -> "홀드 성공했음" 기억
            if (interactAction.WasPerformedThisFrame())
            {
                interactHoldStarted = true;
                _holdTriggeredThisPress = true; // ? 기억해둠!
                Debug.Log("Hold Started!");
            }

            // D. 손을 뗐을 때 -> 홀드 성공한 적 없으면 탭!
            if (interactAction.WasReleasedThisFrame())
            {
                // 이번에 누르고 있는 동안 홀드 발동 안 했어? 그럼 탭이야!
                if (!_holdTriggeredThisPress)
                {
                    interactTapped = true;
                    Debug.Log("Tap!");
                }

                // (손 뗐으니 기억 리셋은 다음 누를 때 처리됨)
            }

            // 3. ECS 배달
            Entities.ForEach((ref PlayerInputComponent playerInput) =>
            {
                playerInput.MoveInput = input;
                playerInput.InteractHolding = interactHolding;
                playerInput.InteractTapped = interactTapped;
                playerInput.InteractHoldStarted = interactHoldStarted;

            }).WithoutBurst().Run();
        }
    }
}