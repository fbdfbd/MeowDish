using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class PlayerInputSystem : SystemBase
    {
        private InputSystem_Actions _inputActions;
        private bool _holdTriggeredThisPress = false;

        protected override void OnCreate()
        {
            RequireForUpdate<PlayerInputComponent>();
            _inputActions = new InputSystem_Actions();
            _inputActions.Enable();
        }

        protected override void OnDestroy()
        {
            _inputActions.Disable();
            _inputActions.Dispose();
        }

        protected override void OnUpdate()
        {
            // 1) 이동 입력
            Vector2 moveVec = _inputActions.Player.Move.ReadValue<Vector2>();
            float2 input = new float2(moveVec.x, moveVec.y);

            // 2) 상호작용 입력
            bool interactTapped = false;
            bool interactHoldStarted = false;
            bool interactHolding = false;

            var interactAction = _inputActions.Player.Interact;

            if (interactAction.WasPressedThisFrame())
            {
                _holdTriggeredThisPress = false;
            }

            interactHolding = interactAction.IsPressed();

            if (interactAction.WasPerformedThisFrame())
            {
                interactHoldStarted = true;
                _holdTriggeredThisPress = true;
            }

            if (interactAction.WasReleasedThisFrame())
            {
                if (!_holdTriggeredThisPress)
                {
                    interactTapped = true;
                }
            }

            // 3) ECS 전달
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
