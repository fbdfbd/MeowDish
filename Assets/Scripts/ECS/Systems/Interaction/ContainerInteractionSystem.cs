using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Meow.ECS.Components;
using Meow.ECS.Utils;
using Meow.Audio;

namespace Meow.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InteractionSystem))]
    public partial struct ContainerInteractionSystem : ISystem
    {
        private int _itemIdCounter;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _itemIdCounter = 1000;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<GamePauseComponent>(out var pause) && pause.IsPaused) return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            int currentItemId = _itemIdCounter;

            foreach (var (request, playerState, playerTransform, entity) in
                     SystemAPI.Query<RefRO<InteractionRequestComponent>, RefRW<PlayerStateComponent>, RefRO<LocalTransform>>()
                              .WithAll<ContainerRequestTag>()
                              .WithEntityAccess())
            {
                Entity stationEntity = request.ValueRO.TargetStation;
                var container = SystemAPI.GetComponent<ContainerComponent>(stationEntity);

                if (!playerState.ValueRO.IsHoldingItem)
                {
                    Entity newItemEntity = ecb.CreateEntity();

                    ecb.AddComponent(newItemEntity, new ItemComponent
                    {
                        ItemID = currentItemId++,
                        Type = ItemType.Ingredient,
                        State = ItemState.Raw,
                        IngredientType = container.ProvidedIngredient
                    });

                    ecb.AddComponent<IngredientTag>(newItemEntity);
                    ecb.AddComponent<RawItemTag>(newItemEntity);

                    if (container.ProvidedIngredient == IngredientType.Meat)
                    {
                        ecb.AddComponent(newItemEntity, new CookableComponent { CookTime = 5f, BurnTime = 8f });
                        ecb.AddComponent<BurnableTag>(newItemEntity);
                        ecb.AddComponent(newItemEntity, new CookingState { Elapsed = 0f });
                    }

                    var stateCopy = playerState.ValueRO;
                    InteractionHelper.AttachItemToPlayer(
                        ref ecb,
                        entity,
                        ref stateCopy,
                        playerTransform.ValueRO.Position,
                        playerTransform.ValueRO.Rotation,
                        newItemEntity,
                        itemHasLocalTransform: false,
                        itemHasHoldable: false
                    );
                    playerState.ValueRW = stateCopy;

                    AppendAudioEvent(stationEntity, ref ecb, ref state, SfxId.Pickup);
                }
                else
                {
                    Entity heldItemEntity = playerState.ValueRO.HeldItemEntity;
                    if (heldItemEntity != Entity.Null && SystemAPI.HasComponent<ItemComponent>(heldItemEntity))
                    {
                        var heldItem = SystemAPI.GetComponent<ItemComponent>(heldItemEntity);

                        bool isMatchingType = heldItem.IngredientType == container.ProvidedIngredient;
                        bool isRawState = heldItem.State == ItemState.Raw;
                        bool canReturn = container.AllowReturn;

                        if (isMatchingType && isRawState && canReturn)
                        {
                            InteractionHelper.DestroyHeldItem(ref ecb, entity, ref playerState.ValueRW);
                            AppendAudioEvent(stationEntity, ref ecb, ref state, SfxId.Pickup);
                        }
                    }
                }

                InteractionHelper.EndRequest<ContainerRequestTag>(ref ecb, entity);
            }

            _itemIdCounter = currentItemId;
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
            buffer.Add(new AudioEvent { Sfx = sfx, Is2D = true });
        }
    }
}
