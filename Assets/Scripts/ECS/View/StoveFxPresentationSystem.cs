using Unity.Entities;
using Unity.Transforms;
using Meow.ECS.Components;
using Meow.Audio;
using Meow.Managers;

namespace Meow.ECS.Systems
{
    /// <summary>
    /// 스토브 FX/사운드 연출
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(StoveCookingSystem))]
    public partial class StoveFxPresentationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (fxBuffer, stoveState, transform) in
                     SystemAPI.Query<DynamicBuffer<StoveFxEvent>, RefRW<StoveCookingState>, RefRO<LocalTransform>>())
            {
                if (fxBuffer.IsEmpty) continue;

                var state = stoveState.ValueRO;
                bool dirty = false;

                foreach (var fx in fxBuffer)
                {
                    switch (fx.Event)
                    {
                        case StoveFxEvent.Kind.StartCook:
                            if (AudioManager.Instance != null && state.SfxLoopHandle == 0)
                            {
                                state.SfxLoopHandle = AudioManager.Instance.PlayLoop(SfxId.Grilling);
                                AudioManager.Instance.PlaySfx2D(SfxId.Pickup);
                                dirty = true;
                            }
                            if (ParticleManager.Instance != null && state.SmokeLoopHandle == 0)
                            {
                                state.SmokeLoopHandle = ParticleManager.Instance.SpawnLoop(
                                    ParticleType.StoveSmoke,
                                    fx.WorldPos,
                                    fx.Rot);
                                dirty = true;
                            }
                            break;

                        case StoveFxEvent.Kind.StopCook:
                            if (state.SfxLoopHandle != 0 && AudioManager.Instance != null)
                            {
                                AudioManager.Instance.StopLoop(state.SfxLoopHandle);
                                state.SfxLoopHandle = 0;
                                dirty = true;
                            }
                            if (state.SmokeLoopHandle != 0 && ParticleManager.Instance != null)
                            {
                                ParticleManager.Instance.DespawnLoop(state.SmokeLoopHandle);
                                state.SmokeLoopHandle = 0;
                                dirty = true;
                            }
                            AudioManager.Instance?.PlaySfx2D(SfxId.Pickup);
                            break;

                        case StoveFxEvent.Kind.Cooked:
                            AudioManager.Instance?.PlaySfx2D(SfxId.Pickup);
                            break;

                        case StoveFxEvent.Kind.Burned:
                            if (state.SfxLoopHandle != 0 && AudioManager.Instance != null)
                            {
                                AudioManager.Instance.StopLoop(state.SfxLoopHandle);
                                state.SfxLoopHandle = 0;
                                dirty = true;
                            }
                            if (state.SmokeLoopHandle != 0 && ParticleManager.Instance != null)
                            {
                                ParticleManager.Instance.DespawnLoop(state.SmokeLoopHandle);
                                state.SmokeLoopHandle = 0;
                                dirty = true;
                            }
                            AudioManager.Instance?.PlaySfx2D(SfxId.Cutting);
                            break;
                    }
                }

                if (dirty)
                    stoveState.ValueRW = state;

                fxBuffer.Clear();
            }
        }
    }
}
