using UnityEngine;
using UnityEngine.UI;
using Meow.Bridge;

namespace Meow.UI
{
    public class UI_GameFlowPanel : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] GameObject panelContent;
        [SerializeField] Button resumeButton;
        [SerializeField] Button quitButton;

        private void Start()
        {
            // ÃÊ±â¿£ ¼û±è
            if (panelContent != null) panelContent.SetActive(false);

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(OnQuitClicked);
            }

            // ºê¸´Áö ¿¬°á(Á¤Áö»óÅÂ¿ë)
            if (GameBridge.Instance != null)
            {
                GameBridge.Instance.OnPauseStateChanged += HandlePauseState;

                HandlePauseState(GameBridge.Instance.IsPaused);
            }
        }

        private void OnDestroy()
        {
            if (GameBridge.Instance != null)
            {
                GameBridge.Instance.OnPauseStateChanged -= HandlePauseState;
            }
        }

        private void HandlePauseState(bool isPaused)
        {
            if (panelContent != null)
            {
                panelContent.SetActive(isPaused);
            }
        }

        private void OnResumeClicked()
        {
            if (GameBridge.Instance != null)
            {
                GameBridge.Instance.TogglePause();
            }
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}