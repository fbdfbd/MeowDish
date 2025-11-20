using Unity.Entities;
using Unity.Mathematics;

namespace Meow.ECS.Components
{
    public struct PlayerInputComponent : IComponentData
    {
        /// <summary>
        /// 조이스틱 오른쪽 : MoveInput = (1.0, 0.0)
        /// </summary>
        public float2 MoveInput;

        /// <summary>
        /// 상호작용 탭 (짧게 한번 누르기)
        /// </summary>
        public bool InteractTapped;

        /// <summary>
        /// 상호작용 홀드 시작 (0.5초 이상 눌렀을 때)
        /// </summary>
        public bool InteractHoldStarted;

        /// <summary>
        /// 상호작용 홀드 중 (계속 누르고 있는 중)
        /// </summary>
        public bool InteractHolding;
    }
}