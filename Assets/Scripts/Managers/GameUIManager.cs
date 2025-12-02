using Unity.Entities;
using UnityEngine;
using TMPro; // TextMeshPro 필수!
using Meow.ECS.Components;

namespace Meow.UI
{
    public class GameUIManager : MonoBehaviour
    {
        [Header("UI 연결")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI customerCountText;
        public TextMeshProUGUI failCountText; // 라이프 (예: ??????)

        private EntityManager _em;
        private EntityQuery _sessionQuery;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            _em = world.EntityManager;

            // GameSessionComponent가 있는 엔티티를 찾는 쿼리
            _sessionQuery = _em.CreateEntityQuery(typeof(GameSessionComponent));
        }

        private void Update()
        {
            // 세션 데이터가 있는지 확인
            if (_sessionQuery.IsEmptyIgnoreFilter) return;

            // 싱글톤 데이터 읽기
            var sessionEntity = _sessionQuery.GetSingletonEntity();
            var session = _em.GetComponentData<GameSessionComponent>(sessionEntity);

            // 1. 점수 표시
            if (scoreText != null)
                scoreText.text = $"Score: {session.CurrentScore:N0}";

            // 2. 남은 손님 수 (처리된 수 / 전체 수)
            if (customerCountText != null)
            {
                int remaining = session.TotalCustomers - session.ProcessedCount;
                customerCountText.text = $"Guests: {remaining} / {session.TotalCustomers}";
            }

            // 3. 실패 횟수 (라이프)
            if (failCountText != null)
            {
                int lives = session.MaxFailures - session.CurrentFailures;
                lives = Mathf.Max(0, lives); // 음수 방지
                failCountText.text = $"Life: {lives}";
            }
        }
    }
}