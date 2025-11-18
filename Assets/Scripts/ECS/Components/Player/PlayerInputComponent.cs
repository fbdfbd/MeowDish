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
        public bool InteractPressed;
    }
}