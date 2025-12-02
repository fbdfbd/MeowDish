using Unity.Entities;
using UnityEngine;
using Meow.ECS.Components;
using System.Collections.Generic;

namespace Meow.ECS.Authoring
{
    public class GameManagerAuthoring : MonoBehaviour
    {
        [Header("스테이지 규칙")]
        public int maxFailures = 3;   // 라이프 3개
        public int totalCustomers = 10; // 손님 10명
        public int stageLevel = 1;

        [Header("초기 버프 (테스트)")]
        public List<BuffType> startingBuffs;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var em = world.EntityManager;
            var entity = em.CreateEntity();

#if UNITY_EDITOR
            em.SetName(entity, "GameSession_Singleton");
#endif

            em.AddComponentData(entity, new GameSessionComponent
            {
                State = GameState.Playing, // 바로 시작 (또는 Ready)
                MaxFailures = maxFailures,
                CurrentFailures = 0,
                TotalCustomers = totalCustomers,
                ServedCustomers = 0,
                ProcessedCount = 0,
                CurrentScore = 0,
                CurrentStageLevel = stageLevel,
                IsStageInitialized = false // 아직 버프 적용 안 됨
            });

            var buffBuffer = em.AddBuffer<ActiveBuffElement>(entity);
            foreach (var buff in startingBuffs)
            {
                buffBuffer.Add(new ActiveBuffElement { Type = buff });
            }
        }
    }
}