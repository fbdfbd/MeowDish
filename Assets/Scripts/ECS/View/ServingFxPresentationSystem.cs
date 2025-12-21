using Unity.Entities;
using Meow.ECS.Components;
using Meow.Managers;

namespace Meow.ECS.Systems
{
    /// <summary>서빙 FX 이벤트를 소비해 파티클 재생</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ServingInteractionSystem))]
    public partial class ServingFxPresentationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (ParticleManager.Instance == null) return;

            foreach (var buffer in SystemAPI.Query<DynamicBuffer<ServingFxEvent>>())
            {
                foreach (var fx in buffer)
                {
                    ParticleManager.Instance.SpawnOneShot(fx.Particle, fx.WorldPos);
                }
                buffer.Clear();
            }
        }
    }
}
