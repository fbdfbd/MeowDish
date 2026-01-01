using System.Collections.Generic;
using UnityEngine;
using Meow.Bootstrap;
using Meow.Run;    // RunManager, DefinitionCatalog, UpgradeLoadout
using Meow.Data;   // RunDefinitionSO, SkillDefinitionSO

public class StageBootstrapper : MonoBehaviour
{
    [Header("Catalog")]
    public DefinitionCatalog catalog; // id → SO 매핑용

    [Header("Fallback (씬 단독 실행용)")]
    public RunDefinitionSO defaultRunDefinition;
    public int defaultStartStageIndex = 0;

    private void Start()
    {
        // RunBootstrap에서 컨텍스트를 가져오거나 fallback 사용
        RunDefinitionSO run = defaultRunDefinition;
        int stageIndex = defaultStartStageIndex;
        UpgradeLoadout loadout = default;

        if (RunBootstrap.Instance != null &&
            RunBootstrap.Instance.TryGet(out var ctx) &&
            ctx != null)
        {
            if (ctx.run != null) run = ctx.run;
            stageIndex = ctx.startStageIndex;
            loadout = ctx.loadout;
        }

        // id → SO 매핑
        if (catalog != null && loadout.skillIds != null)
        {
            loadout.resolvedSkills = new List<SkillDefinitionSO>();
            foreach (var id in loadout.skillIds)
            {
                if (catalog.TryGetSkill(id, out var so))
                {
                    loadout.resolvedSkills.Add(so);
                }
                else
                {
                    Debug.LogWarning($"[StageBootstrapper] skill id not found: {id}");
                }
            }
        }

        var rm = FindAnyObjectByType<RunManager>();
        if (rm == null || run == null)
        {
            Debug.LogError("[StageBootstrapper] RunManager 또는 RunDefinition 없음");
            return;
        }

        rm.StartRun(run, loadout, stageIndex);
    }
}
