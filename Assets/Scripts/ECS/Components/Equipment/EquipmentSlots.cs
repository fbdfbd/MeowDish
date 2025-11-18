using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 장비 슬롯 타입
    /// </summary>
    public enum EquipmentSlot
    {
        Head = 0,       // 모자
        Body = 1,       // 옷
        Hands = 2,      // 장갑
        Feet = 3,       // 신발
        Accessory1 = 4, // 악세서리 1
        Accessory2 = 5  // 악세서리 2
    }

    /// <summary>
    /// 현재 장착 중인 장비 ID
    /// 
    /// 사용법:
    /// 0 = 장비 없음
    /// 101 = 요리사 모자
    /// 102 = 셰프 모자
    /// ...
    /// 
    /// (나중에 ScriptableObject로 관리)
    /// </summary>
    public struct EquipmentSlots : IComponentData
    {
        public int HeadEquipmentID;
        public int BodyEquipmentID;
        public int HandsEquipmentID;
        public int FeetEquipmentID;
        public int Accessory1EquipmentID;
        public int Accessory2EquipmentID;
    }
}