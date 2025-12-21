using System.Collections.Generic;
using Meow.Data;
using Meow.ECS.Components;

namespace Meow.Run
{
    [System.Serializable]
    public class RunState
    {
        public int currentStageIndex = 0;
        public int totalScore = 0;
        public int totalFailures = 0;

        public EquipmentSlots equipmentSlots;
        public List<SkillDefinitionSO> permanentBuffs = new();
        public List<SkillDefinitionSO> temporaryBuffs = new();

        public void Reset()
        {
            currentStageIndex = 0;
            totalScore = 0;
            totalFailures = 0;
            equipmentSlots = default;
            permanentBuffs.Clear();
            temporaryBuffs.Clear();
        }
    }
}
