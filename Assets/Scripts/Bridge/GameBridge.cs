using System;
using System.Collections.Generic;
using Meow.Data;
using Meow.Run;
using Unity.Entities;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.Bridge
{
    public partial class GameBridge : MonoBehaviour
    {
        public static GameBridge Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); }
        }

        // ========================================================================
        // 정지 시스템
        // ========================================================================
        public bool IsPaused { get; private set; } = false;

        // UI 팝업을 띄우기 위한 이벤트
        public event Action<bool> OnPauseStateChanged;

        public void TogglePause()
        {
            // isSystemPause는 false(기본값)
            SetPause(!IsPaused);
        }

        // ECS 동기화
        private void SyncEcsPause(bool shouldPause)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;

            var query = em.CreateEntityQuery(typeof(GamePauseComponent));
            if (query.CalculateEntityCount() == 0)
            {
                var entity = em.CreateEntity(typeof(GamePauseComponent));
                em.SetComponentData(entity, new GamePauseComponent { IsPaused = shouldPause });
                return;
            }

            if (query.TryGetSingletonEntity<GamePauseComponent>(out var pauseEntity))
            {
                em.SetComponentData(pauseEntity, new GamePauseComponent { IsPaused = shouldPause });
            }
        }

        public void SetPause(bool shouldPause, bool isSystemPause = false)
        {
            // ECS 싱글톤이 늦게 생성 대비 동기화
            if (IsPaused == shouldPause)
            {
                SyncEcsPause(shouldPause);
                return;
            }

            IsPaused = shouldPause;

            // 1. 물리/시간 정지
            Time.timeScale = IsPaused ? 0f : 1f;

            // 2. ECS 정지 플래그 갱신
            SyncEcsPause(IsPaused);

            Debug.Log($"[Bridge] Pause: {IsPaused} (System: {isSystemPause})");

            // 3. UI 팝업 이벤트는 시스템 정지 아닐 때만
            // 유저가 버튼 눌렀을 때만 팝업
            if (!isSystemPause)
            {
                OnPauseStateChanged?.Invoke(IsPaused);
            }
        }

        // ========================================================================
        // 스킬 목록 요청
        // ========================================================================
        public Func<IEnumerable<SkillDefinitionSO>> OnGetOwnedSkills;
        public IEnumerable<SkillDefinitionSO> GetOwnedSkills()
        {
            if (OnGetOwnedSkills == null) return null;
            return OnGetOwnedSkills.Invoke();
        }

        // ========================================================================
        // 보상 선택창 제어
        // ========================================================================
        public event Action<List<RewardOption>> OnShowRewardUI;
        public void ShowRewardUI(List<RewardOption> options) => OnShowRewardUI?.Invoke(options);

        public event Action OnHideRewardUI;
        public void HideRewardUI() => OnHideRewardUI?.Invoke();

        public event Action<int> OnRewardSelected;
        public void SelectReward(int index) => OnRewardSelected?.Invoke(index);

        // ========================================================================
        // 게임 결과
        // ========================================================================
        public event Action<bool, int> OnGameResult;
        public void TriggerGameResult(bool isClear, int score) => OnGameResult?.Invoke(isClear, score);

        public event Action OnRestartRequested;
        public void RequestRestart() => OnRestartRequested?.Invoke();



        /// 스테이지클리어이벤트
        public event Action<Action> OnPlayClearEffect;

        // 2. RunManager가 호출할 함수
        public void PlayClearEffect(Action onComplete)
        {
            if (OnPlayClearEffect != null)
            {
                OnPlayClearEffect.Invoke(onComplete);
            }
            else
            {
                Debug.LogWarning("[Bridge] 클리어 연출 UI 연결되지 않음");
                onComplete?.Invoke();
            }
        }
    }
}
