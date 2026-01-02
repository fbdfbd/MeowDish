using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Meow.Run;
using Meow.Bootstrap;

public class LobbyTestStart : MonoBehaviour
{
    [Header("UI")]
    public Button startButton;

    [Header("Stage Preset")]
    public StagePresetCatalogSO stagePresetCatalog;
    public string stageKey;
    public StagePresetSO fallbackPreset; // 카탈로그 실패 시 사용

    [Header("Scene")]
    public string stageSceneName = "Stage_Burger";

    private void Awake()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        StagePresetSO preset = null;
        if (stagePresetCatalog != null && stagePresetCatalog.TryGet(stageKey, out var found))
        {
            preset = found;
        }
        else
        {
            preset = fallbackPreset;
            Debug.LogWarning($"[LobbyTestStart] stageKey '{stageKey}'를 카탈로그에서 찾지 못했습니다. fallback 사용: {preset?.name}");
        }

        if (preset == null || preset.runDefinition == null)
        {
            Debug.LogError("[LobbyTestStart] StagePreset 또는 RunDefinition이 없습니다.");
            return;
        }

        var loadout = preset.defaultLoadout; // id/수치 기반

        RunBootstrap.Instance?.Register(new RunEntryContext
        {
            run = preset.runDefinition,
            startStageIndex = preset.startStageIndex,
            loadout = loadout
        });

        SceneManager.LoadScene(stageSceneName);
    }
}
