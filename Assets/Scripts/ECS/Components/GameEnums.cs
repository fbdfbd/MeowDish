namespace Meow.ECS.Components
{
    public enum PlayerActionState : byte
    {
        Idle = 0,
        Moving = 1,
        Working = 2, // 뭔가 작업 중(이동 불가? 넣말)
        Stunned = 3  // 기절 등 (조작 불가)
    }

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
        Burnt = 3,   // 탐 (실패)
        Dirty = 4,    // 더러움 (접시)
        Clean = 5     // 깨끗함 (접시)
    }

    /// <summary>
    /// 재료 종류
    /// </summary>
    public enum IngredientType
    {
        None = 0,
        Bread = 1,
        Meat = 2,
        Lettuce = 3,
        Tomato = 4,
        Cheese = 5,
        // Plate = 6,

        Burger = 10,    

        // [추가]
        Wrapper = 20,      // 비닐 포장지 (컨테이너에서 꺼냄)
        WrappedBurger = 21 // 최종 완성품 (손님에게 서빙)
    }

    /// <summary>
    /// 스테이션 타입
    /// </summary>
    public enum StationType
    {
        None = 0,
        Container = 1,        // 컨테이너 (무한 재료)
        Counter = 2,          // 임시 보관 테이블 (여러 아이템)
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

    public enum CustomerState : byte
    {
        Spawned,        // 막 태어남
        FindingLine,    // 줄 설 곳 찾는 중
        MovingToLine,   // 이동 중
        WaitingInQueue, // 줄 서서 대기 (평온)
        Ordering,       // 주문 중 (맨 앞)
        WaitingLate,    // ?? 주문 중인데 너무 늦음! (화남/Angry)
        Eating,         // 먹는 중
        Leaving_Happy,  // 성공 퇴장 (행복/Happy)
        Leaving_Angry   // 실패 퇴장 (울음/Cry)
    }
}