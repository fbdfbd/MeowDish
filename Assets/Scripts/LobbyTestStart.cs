using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Meow.Run;
using Meow.Data;
using Meow.Bootstrap;

public class LobbyTestStart : MonoBehaviour
{
    [Header("UI")]
    public Button startButton;

    [Header("Run / Stage")]
    public RunDefinitionSO runDefinition;
    public string stageSceneName = "Stage_Burger";

    [Header("Test Loadout (id)")]
    public List<string> testSkillIds;
    public int bonusGold; 
    public int extraLives;

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

        // 차트데이터로?
        var loadout = new UpgradeLoadout
        {
            skillIds = testSkillIds,
            bonusGold = bonusGold,
            extraLives = extraLives,
            spawnIntervalMultiplier = 1f,
            patienceMultiplier = 1f,
            additionalCustomers = 0,
            scoreMultiplierBonus = 1f
        };

        // RunEntryContext 등록
        RunBootstrap.Instance?.Register(new RunEntryContext
        {
            run = runDefinition,
            startStageIndex = 0,
            loadout = loadout
        });

        // 스테이지 씬 로드
        SceneManager.LoadScene(stageSceneName);
    }
}
