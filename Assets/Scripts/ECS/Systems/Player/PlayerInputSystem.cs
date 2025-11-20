using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Meow.ECS.Components;
using Meow.ScriptableObjects;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// ScriptableObject를 통한 입력 처리
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class PlayerInputSystem : SystemBase
    {
        // ? ScriptableObject 참조
        private InputReferences _inputReferences;

        protected override void OnCreate()
        {
            RequireForUpdate<PlayerInputComponent>();

            // ? Resources 폴더에서 로드
            _inputReferences = Resources.Load<InputReferences>("InputReferences");

            if (_inputReferences == null)
            {
                Debug.LogError("[PlayerInputSystem] InputReferences not found in Resources folder! " +
                    "Create it at 'Assets/Resources/InputReferences.asset'");
            }
            else
            {
                Debug.Log("[PlayerInputSystem] InputReferences loaded successfully!");
            }
        }

        protected override void OnUpdate()
        {
            // ========================================
            // 1. 이동 입력
            // ========================================
            float2 input = float2.zero;

            // ? ScriptableObject를 통해 접근
            if (_inputReferences != null && _inputReferences.joystick != null)
            {
                Vector2 joystickInput = _inputReferences.joystick.InputVector;
                input = new float2(joystickInput.x, joystickInput.y);
            }

            // 키보드 입력 (테스트용)
            if (Input.GetKey(KeyCode.W)) input.y = 1;
            if (Input.GetKey(KeyCode.S)) input.y = -1;
            if (Input.GetKey(KeyCode.A)) input.x = -1;
            if (Input.GetKey(KeyCode.D)) input.x = 1;

            // ========================================
            // 2. 상호작용 입력 (Tap + Hold)
            // ========================================
            bool interactTapped = false;
            bool interactHoldStarted = false;
            bool interactHolding = false;

            // ? ScriptableObject를 통해 접근
            if (_inputReferences != null && _inputReferences.interactionButton != null)
            {
                interactTapped = _inputReferences.interactionButton.WasTappedThisFrame;
                interactHoldStarted = _inputReferences.interactionButton.WasHoldStartedThisFrame;
                interactHolding = _inputReferences.interactionButton.IsHolding;

                if (interactTapped)
                {
                    Debug.Log("[PlayerInputSystem] TAP detected!");
                }
                if (interactHoldStarted)
                {
                    Debug.Log("[PlayerInputSystem] HOLD STARTED!");
                }
            }

            // 키보드 입력 (테스트용)
            if (Input.GetKeyDown(KeyCode.E))
            {
                interactTapped = true;
            }
            if (Input.GetKey(KeyCode.E))
            {
                interactHolding = true;
            }

            // ========================================
            // 3. ECS로 전달
            // ========================================
            Entities.ForEach((ref PlayerInputComponent playerInput) =>
            {
                playerInput.MoveInput = input;
                playerInput.InteractTapped = interactTapped;
                playerInput.InteractHoldStarted = interactHoldStarted;
                playerInput.InteractHolding = interactHolding;
            }).WithoutBurst().Run();
        }
    }
}