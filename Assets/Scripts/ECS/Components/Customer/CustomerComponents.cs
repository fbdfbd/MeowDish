using Unity.Entities;
using UnityEngine;

namespace Meow.ECS.Components
{
    public struct CustomerTag : IComponentData { }

    public struct CustomerComponent : IComponentData
    {
        public int CustomerID;          // 구분용 ID
        public CustomerState State;
        public Entity TargetStation;
        public int QueueIndex;

        public IngredientType OrderDish;
        public float Patience;
        public float MaxPatience;

        public float WalkSpeed;

        public float LeaveTimer;
    }

    /// <summary>
    /// 손님 엔티티와 GameObject 연결
    /// 애니메이터 캐싱
    /// </summary>
    public class CustomerVisualReference : ICleanupComponentData
    {
        public UnityEngine.GameObject VisualObject;
        public Meow.ECS.View.CustomerVisualHelper Helper;
        public CustomerState LastState;
    }



    public struct CustomerSpawnerComponent : IComponentData
    {
        public float SpawnInterval;
        public float Timer;
        public float WalkSpeed;
        public float MaxPatience;

        public int MaxCustomersPerStage;
        public int SpawnedCount;

        public bool IsActive;
    }

    [InternalBufferCapacity(8)]
    public struct PossibleMenuElement : IBufferElementData
    {
        public IngredientType DishType;
    }
}