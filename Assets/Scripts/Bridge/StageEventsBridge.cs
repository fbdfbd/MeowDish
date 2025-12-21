using Unity.Entities;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.Run
{
    public class StageEventsBridge : MonoBehaviour
    {
        private EntityManager _em;
        private GameState _lastState = GameState.Ready;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            _em = world != null ? world.EntityManager : default;
        }

        private void Update()
        {
            if (_em == default) return;

            var query = _em.CreateEntityQuery(typeof(GameSessionComponent));
            if (!query.TryGetSingletonEntity<GameSessionComponent>(out Entity session)) return;

            var data = _em.GetComponentData<GameSessionComponent>(session);
            if (data.State != _lastState)
            {
                Debug.Log($"[StageEventsBridge] state º¯°æ {_lastState} > {data.State}");

                if (RunManager.Instance != null &&
                    (data.State == GameState.StageClear || data.State == GameState.GameOver))
                {
                    Debug.Log($"[StageEventsBridge] RunManager result={data.State}");
                    RunManager.Instance.HandleStageEnd(data.State, data.CurrentScore, data.CurrentFailures);
                }
                _lastState = data.State;
            }
        }
    }
}
