using System.Collections.Generic;
using UnityEngine;
using Meow.Data; 

namespace Meow.Run
{
    [CreateAssetMenu(fileName = "DefinitionCatalog", menuName = "Meow/DefinitionCatalog")]
    public class DefinitionCatalog : ScriptableObject
    {
        [Header("Skill Library (id = skillId)")]
        public List<SkillDefinitionSO> skills = new();

        // 나중에 Equipment 추가
        // List<EquipmentItemSO> equipments = new();

        private Dictionary<string, SkillDefinitionSO> _skillMap;

        void OnEnable() => BuildMaps();

        public void BuildMaps()
        {
            _skillMap = new Dictionary<string, SkillDefinitionSO>();
            foreach (var s in skills)
            {
                if (s == null || string.IsNullOrEmpty(s.skillId)) continue;
                _skillMap[s.skillId] = s;
            }
        }

        public bool TryGetSkill(string id, out SkillDefinitionSO so)
        {
            if (_skillMap == null) BuildMaps();
            return _skillMap.TryGetValue(id, out so);
        }
    }
}
