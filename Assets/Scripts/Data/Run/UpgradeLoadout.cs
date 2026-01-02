using System.Collections.Generic;
using Meow.Data;

namespace Meow.Run
{
    [System.Serializable]
    public struct UpgradeLoadout
    {
        public List<string> skillIds;         // 서버/유저 선택 스킬(id 기반)
        public List<string> equipmentIds;     // 장착 아이템(id 기반)
        public int bonusGold;
        public int extraLives;
        public float spawnIntervalMultiplier; // 1.0 기본, <1 빠름
        public float patienceMultiplier;      // 1.0 기본
        public int additionalCustomers;       // +/-, 기본 0
        public float scoreMultiplierBonus;    // 추가 배율 (1.0 기본)
        public List<string> rewardSkillIds;   // 보상 풀 오버라이드용 id
        public List<string> rewardEquipIds;

        // StageBootstrapper가 id→SO 매핑 후 채워줌
        public List<SkillDefinitionSO> resolvedSkills;
    }
}
