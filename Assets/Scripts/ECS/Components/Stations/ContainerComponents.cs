using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 컨테이너
    /// 
    /// 상호작용 시
    /// - 빈손: 재료 꺼내기
    /// - 들고있음: 반납 시도 (Raw 상태만 가능)
    /// </summary>
    public struct ContainerComponent : IComponentData
    {
        public IngredientType ProvidedIngredient;

        /// <summary>
        /// 반납 허용 여부
        /// </summary>
        public bool AllowReturn;

        /// <summary>
        /// 무한 제공 여부
        /// </summary>
        public bool IsInfinite;
    }
}