using System.Collections.Generic;
using UnityEngine;

namespace Meow.Run
{
    [CreateAssetMenu(fileName = "RunDefinition", menuName = "Meow/RunDefinition")]
    public class RunDefinitionSO : ScriptableObject
    {
        public string runName = "이름";
        public List<StageDefinitionSO> stages = new List<StageDefinitionSO>();

        [Header("스타트 세팅")]
        public List<SkillDefinitionSO> startingBuffs = new List<SkillDefinitionSO>();
        public List<EquipmentItemSO> startingEquipment = new List<EquipmentItemSO>();
    }
}
