using System.Collections.Generic;
using UnityEngine;
using Meow.ECS.Components;
using Meow.Audio;

namespace Meow.Run
{
    [CreateAssetMenu(fileName = "StageDefinition", menuName = "Meow/StageDefinition")]
    public class StageDefinitionSO : ScriptableObject
    {
        [Header("Basic")]
        public string stageName = "Stage_Burger";
        public BgmId bgmId;

        public int stageIndex = 0;
        public int stageLevel = 1;

        [Header("설정")]
        public int totalCustomers = 10;
        public int maxFailures = 3;
        public float spawnInterval = 5f;
        public float customerWalkSpeed = 3f;
        public float customerPatience = 60f;
        public int maxQueueCapacity = 3;
        public float stageTimeLimitSeconds = 0f; // 0 = 무한
        public float scoreMultiplier = 1f;

        [Header("허용 메뉴")]
        public List<IngredientType> menuPool = new List<IngredientType> { IngredientType.Burger };

        [Header("스타팅 버프")]
        public List<SkillDefinitionSO> startingBuffs = new List<SkillDefinitionSO>();

        [Header("보상풀")]
        public List<SkillDefinitionSO> rewardBuffPool = new List<SkillDefinitionSO>();
        public List<EquipmentItemSO> rewardEquipmentPool = new List<EquipmentItemSO>();
        public int rewardChoiceCount = 3;
    }
}
