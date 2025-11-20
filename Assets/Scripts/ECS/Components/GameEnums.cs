namespace Meow.ECS.Components
{
    /// <summary>
    /// 아이템 타입
    /// </summary>
    public enum ItemType
    {
        Ingredient = 0,  // 재료
        Dish = 1,        // 완성 요리
        Plate = 2        // 접시
    }

    /// <summary>
    /// 아이템 상태
    /// </summary>
    public enum ItemState
    {
        Raw = 0,      // 날것 (컨테이너에서 꺼낸 그대로)
        Chopped = 1,  // 썰림
        Cooked = 2,   // 익힘
        Burned = 3,   // 탐 (실패)
        Dirty = 4,    // 더러움 (접시)
        Clean = 5     // 깨끗함 (접시)
    }

    /// <summary>
    /// 재료 종류
    /// </summary>
    public enum IngredientType
    {
        None = 0,
        Bread = 1,    // 빵
        Meat = 2,     // 고기
        Lettuce = 3,  // 양상추
        Tomato = 4,   // 토마토
        Cheese = 5,   // 치즈
        Plate = 6     // 접시
    }

    /// <summary>
    /// 스테이션 타입
    /// </summary>
    public enum StationType
    {
        None = 0,
        Container = 1,        // 컨테이너 (무한 재료)
        WorkBench = 2,        // 임시 보관 테이블 (여러 아이템)
        CuttingBoard = 3,     // 도마 (썰기 전용)
        Stove = 4,            // 가스레인지/오븐 (굽기/조리)
        Sink = 5,             // 싱크대 (세척)
        Assembly = 6,         // 조립대 (요리 조합)
        ServingCounter = 7,   // 서빙 카운터
        TrashCan = 8          // 쓰레기통 (선택)
    }

    /// <summary>
    /// 작업 타입
    /// </summary>
    public enum WorkType
    {
        None = 0,
        Chopping = 1,  // 자르기
        Cooking = 2,   // 조리
        Washing = 3    // 세척
    }
}