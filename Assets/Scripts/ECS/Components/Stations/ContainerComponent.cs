using Unity.Entities;

namespace Meow.ECS.Components
{
    /// <summary>
    /// 컨테이너 (무한 재료 공급)
    /// 
    /// E 키 누르면:
    /// - 빈손: 재료 꺼내기
    /// - 들고있음: 반납 시도 (Raw 상태만 가능)
    /// </summary>
    public struct ContainerComponent : IComponentData
    {
        /// <summary>
        /// 제공하는 재료 종류
        /// </summary>
        public IngredientType ProvidedIngredient;

        /// <summary>
        /// 반납 허용 여부
        /// true = Raw 상태 아이템 다시 넣을 수 있음
        /// </summary>
        public bool AllowReturn;

        /// <summary>
        /// 무한 제공 여부
        /// true = 무제한
        /// </summary>
        public bool IsInfinite;
    }
}