using UnityEngine;
using Meow.Data;

namespace Meow.Run
{
    [CreateAssetMenu(fileName = "StagePreset", menuName = "Meow/StagePreset")]
    public class StagePresetSO : ScriptableObject
    {
        public string stageKey;
        public RunDefinitionSO runDefinition;
        public int startStageIndex = 0;
        public UpgradeLoadout defaultLoadout;
    }
}
