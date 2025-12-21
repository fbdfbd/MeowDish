using Unity.Entities;
using Meow.Bridge; 
using Meow.ECS.Components; 

namespace Meow.ECS.Systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class GameUISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (GameBridge.Instance == null) return;
            if (!SystemAPI.TryGetSingleton<GameSessionComponent>(out var session)) return;


            // 점수
            GameBridge.Instance.UpdateGold(session.CurrentScore);

            // 생명
            int currentLife = session.MaxFailures - session.CurrentFailures;
            GameBridge.Instance.UpdateLife(currentLife, session.MaxFailures);

            // 날짜
            GameBridge.Instance.UpdateDay(session.CurrentStageLevel);

            // 손님 게이지
            GameBridge.Instance.UpdateCustomerData(
                session.ProcessedCount,
                session.TotalCustomers,
                session.TotalCustomers
            );
        }
    }
}