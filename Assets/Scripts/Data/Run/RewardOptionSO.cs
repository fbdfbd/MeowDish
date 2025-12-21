using UnityEngine;
using Meow.Data;

namespace Meow.Run
{
    public enum RewardType { Skill, Equipment }

    [System.Serializable]
    public struct RewardOption
    {
        public RewardType type;
        public SkillDefinitionSO skill;
        public EquipmentItemSO equipment;

        public Sprite Icon => type == RewardType.Skill ? skill?.icon : equipment?.icon;
        public string Title => type == RewardType.Skill ? skill?.displayName : equipment?.displayName;
        public string Description => type == RewardType.Skill ? skill?.description : equipment?.description;
    }
}
