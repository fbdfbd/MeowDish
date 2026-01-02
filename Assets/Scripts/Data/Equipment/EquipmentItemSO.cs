using UnityEngine;
using Meow.ECS.Components;

namespace Meow.Run
{
    [CreateAssetMenu(fileName = "EquipmentItem", menuName = "Meow/Equipment")]
    public class EquipmentItemSO : ScriptableObject
    {
        public string equipmentId = "skillname_num";
        public string displayName = "이름";
        public string description = "설명작성";
        public Sprite icon;
        public EquipmentSlot slot = EquipmentSlot.Head;

        [Header("Stat Bonuses")]
        public float moveSpeedBonus = 0f;
        public float actionSpeedBonus = 0.1f;
        public float scoreBonusMultiplier = 0f; // 예: 0.1 = +10% score
    }
}
