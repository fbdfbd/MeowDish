using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Meow.Bridge;

namespace Meow.UI
{
    public class UI_ResultPanel : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] GameObject panelContent;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text scoreText;

        [Header("Buttons")]
        [SerializeField] Button restartButton;
        [SerializeField] Button quitButton;

        private void Start()
        {
            panelContent.SetActive(false);

            restartButton.onClick.AddListener(() =>
            {
                if (GameBridge.Instance != null) GameBridge.Instance.RequestRestart();
            });

            quitButton.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            if (GameBridge.Instance != null)
            {
                GameBridge.Instance.OnGameResult += ShowResult;
            }
        }

        private void OnDestroy()
        {
            if (GameBridge.Instance != null)
                GameBridge.Instance.OnGameResult -= ShowResult;
        }

        private void ShowResult(bool isClear, int score)
        {
            panelContent.SetActive(true);

            if (isClear)
            {
                titleText.text = "<color=#FFD700>성공!</color>"; 
            }
            else
            {
                titleText.text = "<color=#FF0000>가게가 망했습니다</color>";
            }

            scoreText.text = $"수입: {score}";
        }
    }
}