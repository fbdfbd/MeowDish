using Unity.Entities;
using UnityEngine;
using Meow.ECS.Components;
using System.Collections.Generic;

namespace Meow.ECS.Authoring
{
    public class GameManagerAuthoring : MonoBehaviour
    {
        [Header("스테이지 규칙")]
        public int maxFailures = 3;
        public int totalCustomers = 10;
        public int stageLevel = 1;

        [Header("초기 버프 (테스트)")]
        public List<BuffType> startingBuffs;

        private void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var em = world.EntityManager;

            // 기존에 남아있는 세션 엔티티 제거
            var query = em.CreateEntityQuery(typeof(GameSessionComponent));
            if (!query.IsEmptyIgnoreFilter)
            {
                em.DestroyEntity(query);
            }

            var entity = em.CreateEntity();

#if UNITY_EDITOR
            em.SetName(entity, "GameSession_Singleton");
#endif

            em.AddComponentData(entity, new GameSessionComponent
            {
                State = GameState.Ready,
                MaxFailures = maxFailures,
                CurrentFailures = 0,
                TotalCustomers = totalCustomers,
                ServedCustomers = 0,
                ProcessedCount = 0,
                CurrentScore = 0,
                ScoreMultiplier = 1f,
                CurrentStageLevel = stageLevel,
                IsStageInitialized = false
            });

            var buffBuffer = em.AddBuffer<ActiveBuffElement>(entity);
            if (startingBuffs != null)
            {
                foreach (var buff in startingBuffs)
                {
                    buffBuffer.Add(new ActiveBuffElement { Type = buff });
                }
            }
        }
    }
}