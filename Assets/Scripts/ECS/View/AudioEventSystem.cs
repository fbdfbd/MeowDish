using Unity.Collections;
using Unity.Entities;
using Meow.ECS.Components;
using Meow.Managers;

namespace Meow.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ServingInteractionSystem))]
    [UpdateAfter(typeof(ContainerInteractionSystem))]
    [UpdateAfter(typeof(CounterInteractionSystem))]
    [UpdateAfter(typeof(StoveInteractionSystem))]
    [UpdateAfter(typeof(TrashCanInteractionSystem))]
    [UpdateAfter(typeof(ItemCombinationSystem))]
    public partial class AudioEventSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (AudioManager.Instance == null) return;

            var played = new NativeHashSet<int>(32, Allocator.Temp);

            foreach (var buffer in SystemAPI.Query<DynamicBuffer<AudioEvent>>())
            {
                if (buffer.IsEmpty) continue;

                foreach (var evt in buffer)
                {
                    int key = (int)evt.Sfx;
                    if (evt.AllowDuplicate || played.Add(key))
                    {
                        AudioManager.Instance.PlaySfx2D(evt.Sfx);
                    }
                }

                buffer.Clear();
            }

            played.Dispose();
        }
    }
}
