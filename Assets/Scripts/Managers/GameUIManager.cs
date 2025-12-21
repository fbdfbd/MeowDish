using Unity.Entities;
using UnityEngine;
using TMPro;
using Meow.ECS.Components;

namespace Meow.UI
{
    public class GameUIManager : MonoBehaviour
    {
        [Header("UI 연결")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI customerCountText;
        public TextMeshProUGUI failCountText;

        private EntityManager _em;
        private EntityQuery _sessionQuery;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            _em = world.EntityManager;

            _sessionQuery = _em.CreateEntityQuery(typeof(GameSessionComponent));
        }

        private void Update()
        {
            if (_sessionQuery.IsEmptyIgnoreFilter) return;

            var sessionEntity = _sessionQuery.GetSingletonEntity();
            var session = _em.GetComponentData<GameSessionComponent>(sessionEntity);

            // 1) 점수 표시
            if (scoreText != null)
                scoreText.text = $"Score: {session.CurrentScore:N0}";

            // 2) 남은 손님 수(처리된 수 / 전체 수)
            if (customerCountText != null)
            {
                int remaining = session.TotalCustomers - session.ProcessedCount;
                customerCountText.text = $"Guests: {remaining} / {session.TotalCustomers}";
            }

            // 3) 실패 횟수(생명)
            if (failCountText != null)
            {
                int lives = session.MaxFailures - session.CurrentFailures;
                lives = Mathf.Max(0, lives); // 음수 방지
                failCountText.text = $"Life: {lives}";
            }
        }
    }
}