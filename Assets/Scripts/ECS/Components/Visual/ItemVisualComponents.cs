using Unity.Entities;
using UnityEngine;

namespace Meow.ECS.Components
{
    public class ItemVisualReference : ICleanupComponentData
    {
        public GameObject VisualObject; 
        public IngredientType Type;

        // 색 변경용 상태 기록
        public ItemState LastState;
    }
}