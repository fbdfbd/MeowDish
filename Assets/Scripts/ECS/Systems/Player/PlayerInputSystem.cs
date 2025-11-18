using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Meow.ECS.Components;
using Meow.UI;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// VirtualJoystick 입력을 ECS로 전달
    /// 
    /// MonoBehaviour 참조 때문에 Burst 사용 불가
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class PlayerInputSystem : SystemBase
    {
        private GameObject joystickObject;
        private bool isInitialized;

        protected override void OnCreate()
        {
            RequireForUpdate<PlayerInputComponent>();
        }

        protected override void OnStartRunning()
        {
            if (!isInitialized)
            {
                // VirtualJoystick 찾기
                joystickObject = GameObject.Find("Joystick Background");
                if (joystickObject != null)
                {
                    Debug.Log("[PlayerInputSystem] VirtualJoystick found!");
                    isInitialized = true;
                }
                else
                {
                    Debug.LogWarning("[PlayerInputSystem] VirtualJoystick not found! " +
                        "Create UI with name 'Joystick Background'");
                }
            }
        }

        protected override void OnUpdate()
        {
            // VirtualJoystick이 없으면 키보드 입력 사용
            float2 input = float2.zero;
            bool interactPressed = false;

            if (joystickObject != null)
            {
                // VirtualJoystick에서 입력 읽기
                var joystick = joystickObject.GetComponent<VirtualJoystick>();
                if (joystick != null)
                {
                    Vector2 joystickInput = joystick.InputVector;
                    input = new float2(joystickInput.x, joystickInput.y);
                }
            }

            // 키보드 입력 (테스트용)
            if (Input.GetKey(KeyCode.W)) input.y = 1;
            if (Input.GetKey(KeyCode.S)) input.y = -1;
            if (Input.GetKey(KeyCode.A)) input.x = -1;
            if (Input.GetKey(KeyCode.D)) input.x = 1;

            // ????????????????????????????????????????
            // ?? 상호작용 버튼 - E키로 변경!
            // ????????????????????????????????????????
            interactPressed = Input.GetKeyDown(KeyCode.E);  // Space → E

            // 디버그 (E키 누르면 로그)
            if (interactPressed)
            {
                Debug.Log("[PlayerInputSystem] E key pressed!");
            }

            // ECS로 전달
            Entities.ForEach((ref PlayerInputComponent playerInput) =>
            {
                playerInput.MoveInput = input;
                playerInput.InteractPressed = interactPressed;
            }).WithoutBurst().Run();
        }
    }
}