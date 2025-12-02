using Unity.Entities;
using UnityEngine;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 엔티티와 연결된 GameObject(Visual)를 기억하는 청소용 컴포넌트
    /// </summary>
    public class ItemVisualReference : ICleanupComponentData
    {
        public GameObject VisualObject; // 실제 눈에 보이는 GameObject
        public IngredientType Type;     // 반납할 때 어떤 풀(Pool)에 넣을지 기억

        // ?? [추가] 마지막 상태 기억용 (최적화)
        // 이 값이 현재 상태와 다를 때만 색깔을 바꿉니다.
        public ItemState LastState;
    }
}