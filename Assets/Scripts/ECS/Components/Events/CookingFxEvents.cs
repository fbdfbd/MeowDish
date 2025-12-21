using Meow.Audio;
using Meow.Managers;
using Unity.Entities;
using Unity.Mathematics;

namespace Meow.ECS.Components
{
    public struct StoveFxEvent : IBufferElementData
    {
        public enum Kind : byte { StartCook, StopCook, Cooked, Burned }
        public Kind Event;
        public float3 WorldPos;
        public quaternion Rot;
        public Entity Item;
    }

    public struct AudioEvent : IBufferElementData
    {
        public SfxId Sfx;
        public bool Is2D;
        public bool AllowDuplicate; // true: 같은 프레임에 같은 SFX도 중복 재생
    }

    public struct ServingFxEvent : IBufferElementData
    {
        public ParticleType Particle;
        public float3 WorldPos;
    }
}

