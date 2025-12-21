using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Meow.ECS.Components;

namespace Meow.ECS.Utils
{
    /// <summary>
    /// 상호작용 공통 유틸(소유권 전환, 위치 스냅, 요청 종료)
    /// </summary>
    public static class InteractionHelper
    {
        private static readonly float3 HandOffset = new float3(0f, 1.5f, 0.5f);

        // ------------------------------------------------------------------
        // 내부 헬퍼: LocalTransform, Holdable Add/Set 분기
        // ------------------------------------------------------------------
        private static void SetOrAddLocalTransform(ref EntityCommandBuffer ecb, Entity item, in LocalTransform lt, bool hasLocalTransform)
        {
            if (hasLocalTransform) ecb.SetComponent(item, lt);
            else ecb.AddComponent(item, lt);
        }

        private static void SetOrAddHoldable(ref EntityCommandBuffer ecb, Entity item, Entity owner, bool hasHoldable)
        {
            var holdable = new HoldableComponent { HolderEntity = owner };
            if (hasHoldable) ecb.SetComponent(item, holdable);
            else ecb.AddComponent(item, holdable);
        }

        // 아이템을 플레이어 손에 붙임.
        // itemHasLocalTransform/itemHasHoldable는 대상 아이템이 이미 해당 컴포넌트를 갖고 있는지 여부
        public static void AttachItemToPlayer(
            ref EntityCommandBuffer ecb,
            Entity playerEntity,
            ref PlayerStateComponent playerState, // 복사본 or RefRW
            float3 playerPosition,
            quaternion playerRotation,
            Entity itemEntity,
            bool itemHasLocalTransform,
            bool itemHasHoldable)
        {
            float3 finalPos = playerPosition + math.rotate(playerRotation, HandOffset);

            SetOrAddLocalTransform(ref ecb, itemEntity, new LocalTransform
            {
                Position = finalPos,
                Rotation = playerRotation,
                Scale = 1f
            }, itemHasLocalTransform);

            SetOrAddHoldable(ref ecb, itemEntity, playerEntity, itemHasHoldable);

            playerState.IsHoldingItem = true;
            playerState.HeldItemEntity = itemEntity;
            ecb.SetComponent(playerEntity, playerState); // RefRW/복사본 변경사항을 ECS에 반영
        }

        // 플레이어 손에서 스테이션으로 놓기
        public static void DetachItemFromPlayer(
            ref EntityCommandBuffer ecb,
            Entity playerEntity,
            ref PlayerStateComponent playerState,
            Entity itemEntity,
            Entity newOwnerStation,
            float3 targetWorldPos,
            bool itemHasLocalTransform,
            bool itemHasHoldable)
        {
            SetOrAddLocalTransform(ref ecb, itemEntity, new LocalTransform
            {
                Position = targetWorldPos,
                Rotation = quaternion.identity,
                Scale = 1f
            }, itemHasLocalTransform);

            SetOrAddHoldable(ref ecb, itemEntity, newOwnerStation, itemHasHoldable);

            playerState.IsHoldingItem = false;
            playerState.HeldItemEntity = Entity.Null;
            ecb.SetComponent(playerEntity, playerState);
        }

        // 플레이어가 들고 있는 아이템 제거(서빙, 쓰레기통)
        public static void DestroyHeldItem(
            ref EntityCommandBuffer ecb,
            Entity playerEntity,
            ref PlayerStateComponent playerState)
        {
            if (playerState.HeldItemEntity != Entity.Null)
            {
                ecb.DestroyEntity(playerState.HeldItemEntity);
            }

            playerState.IsHoldingItem = false;
            playerState.HeldItemEntity = Entity.Null;
            ecb.SetComponent(playerEntity, playerState);
        }

        // 요청 종료: 공통 InteractionRequest + 특정 태그 제거
        public static void EndRequest<TTag>(
            ref EntityCommandBuffer ecb,
            Entity playerEntity) where TTag : unmanaged, IComponentData
        {
            ecb.RemoveComponent<InteractionRequestComponent>(playerEntity);
            ecb.RemoveComponent<TTag>(playerEntity);
        }
    }
}
