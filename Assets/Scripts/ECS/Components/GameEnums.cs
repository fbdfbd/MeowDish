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
        Burnt = 3,    // 탐 (실패)
        Dirty = 4,    // 더러움 (접시)
        Clean = 5,    // 깨끗함 (접시)
        Prepared = 6
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
        Wrapper = 20, 
        WrappedBurger = 21 
    }

    /// <summary>
    /// 스테이션 타입
    /// </summary>
    public enum StationType
    {
        None = 0,
        Container = 1,
        Counter = 2,
        CuttingBoard = 3,
        Stove = 4,
        Sink = 5,
        Assembly = 6,
        ServingCounter = 7,
        TrashCan = 8
    }

    /// <summary>
    /// 작업 타입
    /// </summary>
    public enum WorkType
    {
        None = 0,
        Chopping = 1,
        Cooking = 2,
        Washing = 3
    }

    public enum CustomerState : byte
    {
        Spawned,        // 막 태어남
        FindingLine,    // 줄 설 곳 찾는 중
        MovingToLine,   // 이동 중
        WaitingInQueue, // 줄 대기
        Ordering,       // 주문 중(맨 앞)
        WaitingLate,    // 주문 중 늦음(빨간불 게이지, 애니메이션)
        Eating,
        Leaving_Happy,  // 성공 퇴장
        Leaving_Angry   // 실패 퇴장
    }
}