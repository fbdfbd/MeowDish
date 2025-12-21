using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 아이템 기본 데이터
    /// </summary>
    public struct ItemComponent : IComponentData
    {
        /// <summary>
        /// 아이템 고유 ID (생성 시 자동 부여)
        /// </summary>
        public int ItemID;

        /// <summary>
        /// 아이템 타입 (enum)
        /// </summary>
        public ItemType Type;

        /// <summary>
        /// 현재 상태 (Raw, Chopped, Cooked...)
        /// </summary>
        public ItemState State;

        /// <summary>
        /// 재료 종류 (Type이 Ingredient일 때만 유효)
        /// </summary>
        public IngredientType IngredientType;
    }

    // 아이템 분류 태그
    #region Type Tags
    public struct IngredientTag : IComponentData { } // 재료
    public struct DishTag : IComponentData { }       // 완성 요리
    public struct PlateTag : IComponentData { }      // 접시
    #endregion

    // 아이템 상태 태그
    #region State Tags
    public struct RawItemTag : IComponentData { }
    public struct ChoppedItemTag : IComponentData { } 
    public struct MixedItemTag : IComponentData { }
    public struct CookedItemTag : IComponentData { }
    public struct BurnedItemTag : IComponentData { }
    #endregion
}