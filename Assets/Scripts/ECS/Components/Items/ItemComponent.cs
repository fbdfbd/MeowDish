using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 아이템 데이터
    /// 
    /// 모든 아이템(재료, 요리, 접시)에 붙는 Component
    /// </summary>
    public struct ItemComponent : IComponentData
    {
        /// <summary>
        /// 아이템 고유 ID (생성 시 자동 부여)
        /// </summary>
        public int ItemID;

        /// <summary>
        /// 아이템 타입
        /// </summary>
        public ItemType Type;

        /// <summary>
        /// 현재 상태 (Raw, Chopped, Cooked...)
        /// </summary>
        public ItemState State;

        /// <summary>
        /// 재료 종류 (Type이 Ingredient일 때만)
        /// </summary>
        public IngredientType IngredientType;
    }
}