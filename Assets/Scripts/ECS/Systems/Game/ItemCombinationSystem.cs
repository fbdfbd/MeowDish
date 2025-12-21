using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Meow.ECS.Components;
using Meow.ECS.Data.Recipes;
using Meow.Audio;

namespace Meow.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InteractionSystem))]
    [UpdateBefore(typeof(ContainerInteractionSystem))]
    [UpdateBefore(typeof(CounterInteractionSystem))]
    [UpdateBefore(typeof(StoveInteractionSystem))]
    public partial struct ItemCombinationSystem : ISystem
    {
        [BurstCompile] public void OnCreate(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<GamePauseComponent>(out var pause) && pause.IsPaused) return;

            RecipeLookup recipeLookup = default;
            bool hasLookup = false;
            foreach (var lookupRO in SystemAPI.Query<RefRO<RecipeLookup>>())
            {
                recipeLookup = lookupRO.ValueRO;
                hasLookup = true;
                break;
            }
            if (!hasLookup || !recipeLookup.Blob.IsCreated) return;

            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (playerState, request, playerTransform, entity) in
                     SystemAPI.Query<RefRW<PlayerStateComponent>, RefRO<InteractionRequestComponent>, RefRO<LocalTransform>>()
                              .WithEntityAccess())
            {
                if (!playerState.ValueRO.IsHoldingItem) continue;

                Entity heldItemEntity = playerState.ValueRO.HeldItemEntity;
                if (heldItemEntity == Entity.Null || !em.HasComponent<ItemComponent>(heldItemEntity)) continue;

                var heldItemData = em.GetComponentData<ItemComponent>(heldItemEntity);
                Entity targetStation = request.ValueRO.TargetStation;

                ItemComponent targetItemData = new ItemComponent { IngredientType = IngredientType.None };
                Entity targetItemEntity = Entity.Null;
                bool isTargetRealEntity = false;

                // 컨테이너
                if (em.HasComponent<ContainerComponent>(targetStation))
                {
                    var container = em.GetComponentData<ContainerComponent>(targetStation);
                    targetItemData.IngredientType = container.ProvidedIngredient;
                    targetItemData.State = ItemState.Raw;
                }
                // 카운터
                else if (em.HasComponent<CounterComponent>(targetStation))
                {
                    var buffer = em.GetBuffer<CounterItemSlot>(targetStation);
                    if (buffer.Length > 0)
                    {
                        targetItemEntity = buffer[buffer.Length - 1].ItemEntity;
                        if (em.Exists(targetItemEntity))
                        {
                            targetItemData = em.GetComponentData<ItemComponent>(targetItemEntity);
                            isTargetRealEntity = true;
                        }
                    }
                }
                // 스토브
                else if (em.HasComponent<StoveCookingState>(targetStation))
                {
                    var stoveState = em.GetComponentData<StoveCookingState>(targetStation);
                    if (stoveState.ItemEntity != Entity.Null && em.Exists(stoveState.ItemEntity))
                    {
                        targetItemEntity = stoveState.ItemEntity;
                        targetItemData = em.GetComponentData<ItemComponent>(targetItemEntity);
                        isTargetRealEntity = true;
                    }
                }

                if (targetItemData.IngredientType == IngredientType.None) continue;

                if (!TryFindRecipeOutput(recipeLookup.Blob, heldItemData, targetItemData,
                        out ItemType outItemType, out IngredientType outType, out ItemState outState))
                {
                    continue;
                }

                bool targetIsStove = em.HasComponent<StoveCookingState>(targetStation);

                // 스토브 대상이 아닐 때만 즉시 픽업 SFX(스토브는 StopCook 이벤트)
                if (!targetIsStove)
                {
                    AppendAudioEvent(targetStation, ref ecb, ref state, SfxId.Pickup);
                }

                // 스토브 재료 소모
                ecb.DestroyEntity(heldItemEntity);

                if (isTargetRealEntity && targetItemEntity != Entity.Null)
                {
                    ecb.DestroyEntity(targetItemEntity);

                    if (em.HasComponent<CounterComponent>(targetStation))
                    {
                        var buffer = em.GetBuffer<CounterItemSlot>(targetStation);
                        if (buffer.Length > 0) buffer.RemoveAt(buffer.Length - 1);
                    }
                    if (targetIsStove)
                    {
                        var stoveState = em.GetComponentData<StoveCookingState>(targetStation);
                        stoveState.ItemEntity = Entity.Null;
                        stoveState.IsCooking = false;
                        stoveState.CurrentCookProgress = 0;
                        ecb.SetComponent(targetStation, stoveState);

                        var stoveFx = EnsureFxBuffer(targetStation, ref ecb, ref state);
                        var stoveTransform = em.GetComponentData<LocalTransform>(targetStation);
                        stoveFx.Add(new StoveFxEvent
                        {
                            Event = StoveFxEvent.Kind.StopCook,
                            WorldPos = stoveTransform.Position,
                            Rot = stoveTransform.Rotation,
                            Item = targetItemEntity
                        });
                    }
                }

                // 결과물 생성
                Entity resultItem = ecb.CreateEntity();
                ecb.AddComponent(resultItem, new ItemComponent
                {
                    ItemID = heldItemData.ItemID,
                    Type = outItemType,
                    State = outState,
                    IngredientType = outType
                });
                ecb.AddComponent(resultItem, new HoldableComponent { HolderEntity = entity });

                float3 handPos = playerTransform.ValueRO.Position + new float3(0, 1.5f, 0.5f);
                ecb.AddComponent(resultItem, new LocalTransform
                {
                    Position = handPos,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });

                if (outItemType == ItemType.Dish)
                {
                    ecb.AddComponent<DishTag>(resultItem);
                }

                var ps = playerState.ValueRO;
                ps.HeldItemEntity = resultItem;
                ps.IsHoldingItem = true;
                ecb.SetComponent(entity, ps);

                // 요청 종료
                ecb.RemoveComponent<InteractionRequestComponent>(entity);
                if (em.HasComponent<ContainerRequestTag>(entity)) ecb.RemoveComponent<ContainerRequestTag>(entity);
                if (em.HasComponent<CounterRequestTag>(entity)) ecb.RemoveComponent<CounterRequestTag>(entity);
                if (em.HasComponent<StoveRequestTag>(entity)) ecb.RemoveComponent<StoveRequestTag>(entity);
                if (em.HasComponent<CuttingBoardRequestTag>(entity)) ecb.RemoveComponent<CuttingBoardRequestTag>(entity);
            }

            ecb.Playback(em);
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

        private static DynamicBuffer<StoveFxEvent> EnsureFxBuffer(Entity stoveEntity, ref EntityCommandBuffer ecb, ref SystemState state)
        {
            if (state.EntityManager.HasBuffer<StoveFxEvent>(stoveEntity))
                return state.EntityManager.GetBuffer<StoveFxEvent>(stoveEntity);
            return ecb.AddBuffer<StoveFxEvent>(stoveEntity);
        }

        private static bool TryFindRecipeOutput(
            in BlobAssetReference<RecipeBlob> blobRef,
            in ItemComponent held,
            in ItemComponent target,
            out ItemType outItemType,
            out IngredientType outType,
            out ItemState outState)
        {
            outItemType = ItemType.Ingredient;
            outType = IngredientType.None;
            outState = ItemState.Raw;

            ushort a = RecipeBlobBuilder.PackKey(held.IngredientType, held.State);
            ushort b = RecipeBlobBuilder.PackKey(target.IngredientType, target.State);

            ref var root = ref blobRef.Value;
            ref var entries = ref root.Entries;

            for (int i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries[i];
                ref var inputs = ref entry.Inputs;

                if (inputs.Length != 2) continue;

                bool match;
                if (entry.OrderIndependent != 0)
                {
                    ushort x = a, y = b;
                    if (x > y) { ushort tmp = x; x = y; y = tmp; }
                    match = (inputs[0] == x && inputs[1] == y);
                }
                else
                {
                    match = (inputs[0] == a && inputs[1] == b);
                }

                if (!match) continue;

                ItemType decodedType = (ItemType)entry.OutputItemType;
                if (decodedType != ItemType.Ingredient &&
                    decodedType != ItemType.Dish &&
                    decodedType != ItemType.Plate)
                {
                    decodedType = ItemType.Ingredient;
                }

                outItemType = decodedType;

                ushort ok = entry.OutputKey;
                outType = (IngredientType)(ok >> 8);
                outState = (ItemState)(ok & 0xFF);
                return true;
            }

            return false;
        }
    }
}
