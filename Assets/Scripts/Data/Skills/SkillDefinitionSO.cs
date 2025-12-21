using UnityEngine;
using Meow.ECS.Components;

namespace Meow.Data
{
    public enum EffectOp
    {
        Add,
        Multiply
    }

    public enum SkillStackPolicy
    {
        Replace,   
        StackAdd,  
        StackMul   
    }

    [CreateAssetMenu(fileName = "SkillDefinition", menuName = "Meow/Skill")]
    public class SkillDefinitionSO : ScriptableObject
    {
        public SkillRank skillRank;
        public string skillId = "skillid_num";
        public string displayName = "스킬이름";
        public string description = "설명";
        public string positiveText = "";
        public string negativeText = "";
        public Sprite icon;

        [Header("Effect")]
        public BuffType buffType = BuffType.SpeedUp;
        public EffectOp valueOp = EffectOp.Multiply;
        public float multiplier = 1.2f;
        public int flatValue = 0; 
        public float duration = 0f; // 0 = 무한
        public bool isPermanent = false;
        public SkillStackPolicy stackPolicy = SkillStackPolicy.Replace;

        [Header("Negative Effect")]
        public int additionalCustomers = 0; 
    }

    public enum SkillRank
    {
        Common,
        Rare,
        Epic
    }

}
