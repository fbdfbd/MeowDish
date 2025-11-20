using UnityEngine;
using Meow.UI;

namespace Meow.ScriptableObjects
{
    /// <summary>
    /// UI 입력 컴포넌트 참조 관리
    /// </summary>
    [CreateAssetMenu(fileName = "InputReferences", menuName = "Ref/Input References")]
    public class InputReferences : ScriptableObject
    {
        [Header("Input Components")]
        [Tooltip("가상 조이스틱 (씬에 있는 실제 오브젝트)")]
        public VirtualJoystick joystick;

        [Tooltip("상호작용 버튼 (씬에 있는 실제 오브젝트)")]
        public InteractionButton interactionButton;

        /// <summary>
        /// 모든 참조가 유효한지 체크
        /// </summary>
        public bool IsValid()
        {
            return joystick != null && interactionButton != null;
        }

        /// <summary>
        /// 디버그 정보
        /// </summary>
        public void LogStatus()
        {
            Debug.Log($"[InputReferences] Joystick: {(joystick != null ? "?" : "?")}, " +
                      $"InteractionButton: {(interactionButton != null ? "?" : "?")}");
        }
    }
}