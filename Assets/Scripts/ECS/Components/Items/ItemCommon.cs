using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 아이템 기본 데이터
    /// 
    /// 모든 아이템(재료, 요리, 접시)에 필수적으로 붙는 Component
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

    // --- 아이템 분류 태그 (Identity) ---
    #region Type Tags
    public struct IngredientTag : IComponentData { } // 재료
    public struct DishTag : IComponentData { }       // 완성 요리
    public struct PlateTag : IComponentData { }      // 접시
    #endregion

    // --- 아이템 상태 태그 (State) ---
    // 쿼리 필터링을 빠르게 하기 위해 사용 (예: RawItemTag가 있는 것만 굽기 가능)
    #region State Tags
    public struct RawItemTag : IComponentData { }     // 날 것
    public struct ChoppedItemTag : IComponentData { } // 썰린 상태
    public struct MixedItemTag : IComponentData { }   // 섞인 상태
    public struct CookedItemTag : IComponentData { }  // 조리 완료
    public struct BurnedItemTag : IComponentData { }  // 탄 상태
    #endregion
}