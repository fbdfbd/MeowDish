using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Meow.ECS.Components;
using Meow.ECS.Utils;
using Meow.Audio;

namespace Meow.ECS.Systems
{
    /// <summary>쓰레기통: 들고 있는 아이템 삭제</summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InteractionSystem))]
    public partial struct TrashCanInteractionSystem : ISystem
    {
        [BurstCompile] public void OnCreate(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<GamePauseComponent>(out var pause) && pause.IsPaused) return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (request, playerState, entity) in
                     SystemAPI.Query<RefRO<InteractionRequestComponent>, RefRW<PlayerStateComponent>>()
                              .WithAll<TrashCanRequestTag>()
                              .WithEntityAccess())
            {
                if (request.ValueRO.TargetStation == Entity.Null)
                {
                    InteractionHelper.EndRequest<TrashCanRequestTag>(ref ecb, entity);
                    continue;
                }

                if (playerState.ValueRO.IsHoldingItem)
                {
                    var stateCopy = playerState.ValueRW;
                    InteractionHelper.DestroyHeldItem(ref ecb, entity, ref stateCopy);
                    playerState.ValueRW = stateCopy;

                    AppendAudioEvent(request.ValueRO.TargetStation, ref ecb, ref state, SfxId.Cutting);
                }

                InteractionHelper.EndRequest<TrashCanRequestTag>(ref ecb, entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private static DynamicBuffer<AudioEvent> EnsureAudioBuffer(Entity target, ref EntityCommandBuffer ecb, ref SystemState state)
        {
            if (state.EntityManager.HasBuffer<AudioEvent>(target))
                return state.EntityManager.GetBuffer<AudioEvent>(target);
            return ecb.AddBuffer<AudioEvent>(target);
        }

        private static void AppendAudioEvent(Entity target, ref EntityCommandBuffer ecb, ref SystemState state, SfxId sfx)
        {
            var buffer = EnsureAudioBuffer(target, ref ecb, ref state);
            buffer.Add(new AudioEvent { Sfx = sfx, Is2D = true, AllowDuplicate = false });
        }
    }
}
