using UnityEngine;
using Meow.ScriptableObjects;
using Meow.UI;

namespace Meow.Managers
{
    /// <summary>
    /// 씬의 UI를 InputReferences에 자동 연결
    /// </summary>
    public class InputReferencesManager : MonoBehaviour
    {
        [Header("ScriptableObject")]
        [Tooltip("Project에서 만든 InputReferences 에셋")]
        [SerializeField] private InputReferences inputReferences;

        [Header("Auto Find Settings")]
        [SerializeField] private bool autoFindOnStart = true;

        private void Start()
        {
            if (autoFindOnStart)
            {
                FindAndAssignReferences();
            }
        }

        /// <summary>
        /// 씬에서 UI 컴포넌트를 찾아서 ScriptableObject에 할당
        /// </summary>
        public void FindAndAssignReferences()
        {
            if (inputReferences == null)
            {
                Debug.LogError("[InputReferencesManager] InputReferences asset is not assigned!");
                return;
            }

            // VirtualJoystick 찾기
            var joystick = FindFirstObjectByType<VirtualJoystick>();
            if (joystick != null)
            {
                inputReferences.joystick = joystick;
                Debug.Log("[InputReferencesManager] ? VirtualJoystick assigned");
            }
            else
            {
                Debug.LogWarning("[InputReferencesManager] ? VirtualJoystick not found in scene");
            }

            // InteractionButton 찾기
            var button = FindFirstObjectByType<InteractionButton>();
            if (button != null)
            {
                inputReferences.interactionButton = button;
                Debug.Log("[InputReferencesManager] ? InteractionButton assigned");
            }
            else
            {
                Debug.LogWarning("[InputReferencesManager] ? InteractionButton not found in scene");
            }

            inputReferences.LogStatus();
        }

        private void OnDestroy()
        {
            // ? 씬 종료 시 참조 정리 (중요!)
            if (inputReferences != null)
            {
                inputReferences.joystick = null;
                inputReferences.interactionButton = null;
            }
        }

#if UNITY_EDITOR
        // ? Inspector에서 버튼으로 테스트 가능
        [ContextMenu("Find And Assign References")]
        private void EditorFindReferences()
        {
            FindAndAssignReferences();
        }
#endif
    }
}