using Unity.Entities;
using UnityEngine;

namespace Meow.ECS.Components
{
    public struct CustomerTag : IComponentData { }

    public struct CustomerComponent : IComponentData
    {
        public int CustomerID;          // 구분용 ID
        public CustomerState State;     // 현재 상태
        public Entity TargetStation;    // 내가 줄 설 서빙 카운터
        public int QueueIndex;          // 대기열 몇 번째?

        public IngredientType OrderDish; // 주문한 메뉴 (예: Burger)
        public float Patience;           // 현재 인내심
        public float MaxPatience;        // 최대 인내심

        public float WalkSpeed;          // 이동 속도

        public float LeaveTimer;
    }

    /// <summary>
    /// 손님 엔티티와 GameObject(비주얼) 연결고리
    /// + 애니메이터 캐싱
    /// </summary>
    public class CustomerVisualReference : ICleanupComponentData
    {
        public UnityEngine.GameObject VisualObject;
        public Meow.ECS.View.CustomerVisualHelper Helper;
        public CustomerState LastState;
    }


    // 손님 생성
    public struct CustomerSpawnerComponent : IComponentData
    {
        public float SpawnInterval;
        public float Timer;
        public float WalkSpeed;
        public float MaxPatience;

        public int MaxCustomersPerStage; // 총 방문할 손님 수 (예: 10명)
        public int SpawnedCount;         // 지금까지 생성된 손님 수
    }

    [InternalBufferCapacity(8)]
    public struct PossibleMenuElement : IBufferElementData
    {
        public IngredientType DishType;
    }
}