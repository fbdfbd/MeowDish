using System.Collections.Generic;
using UnityEngine;
using Meow.Data;
using Meow.Bridge;

namespace Meow.Run
{
    public class StageRewardController : MonoBehaviour
    {
        [Header("Refs")]
        public RunManager runManager;

        [Header("Config")]
        public int choiceCount = 3;
        public int rerollLimit = 1;
        public bool allowDuplicates = false;

        private readonly List<RewardOption> _pool = new();
        private readonly List<RewardOption> _currentChoices = new();
        private int _rerollRemaining;

        private void Awake()
        {
            if (runManager == null) runManager = FindAnyObjectByType<RunManager>();
        }

        private void Start()
        {
            if (GameBridge.Instance != null)
            {
                GameBridge.Instance.OnRewardSelected += HandleUserSelection;
            }
        }

        private void OnDisable()
        {
            if (GameBridge.Instance != null)
            {
                GameBridge.Instance.OnRewardSelected -= HandleUserSelection;
            }
        }

        public void TriggerRewardForStage(StageDefinitionSO stage)
        {
            Debug.Log("[RewardController] 보상 계산 및 UI 표시 시작");
            _rerollRemaining = rerollLimit;

            BuildPool(stage);
            PickChoices();

            if (GameBridge.Instance != null)
            {
                GameBridge.Instance.SetPause(true, isSystemPause: true);

                GameBridge.Instance.ShowRewardUI(_currentChoices);
            }
        }

        private void HandleUserSelection(int index)
        {
            if (runManager == null) return;
            if (index < 0 || index >= _currentChoices.Count) return;

            var choice = _currentChoices[index];
            Debug.Log($"[RewardController] 보상 지급: {choice.Title}");

            if (choice.type == RewardType.Skill && choice.skill != null)
                runManager.GrantReward(choice.skill);
            else if (choice.type == RewardType.Equipment && choice.equipment != null)
                runManager.GrantReward(choice.equipment);

            if (GameBridge.Instance != null)
            {
                GameBridge.Instance.HideRewardUI();

                GameBridge.Instance.SetPause(false, isSystemPause: true);
            }

            Debug.Log("다음 스테이지로 진입");
            runManager.NextStage();
        }

        private void BuildPool(StageDefinitionSO stage) 
        { 
            _pool.Clear(); 
            if (stage.rewardBuffPool != null) 
                foreach (var s in stage.rewardBuffPool) 
                    _pool.Add(new RewardOption { type = RewardType.Skill, skill = s }); 
            if (stage.rewardEquipmentPool != null) foreach (var e in stage.rewardEquipmentPool) 
                    _pool.Add(new RewardOption { type = RewardType.Equipment, equipment = e }); 
            if (!allowDuplicates) RemoveOwnedDuplicates(); 
        }

        private void RemoveOwnedDuplicates() 
        { 
            if (runManager == null) return; 
            var owned = runManager.GetOwnedSkills(); 
            if (owned == null) return; HashSet<string> ids = new(); 
            foreach (var s in owned) 
                ids.Add(s.skillId); 
            _pool.RemoveAll(x => x.type == RewardType.Skill && x.skill != null && ids.Contains(x.skill.skillId)); 
        }

        private void PickChoices() 
        { 
            _currentChoices.Clear(); 
            if (_pool.Count > 0) 
            { 
                Shuffle(_pool); 
                for (int i = 0; i < choiceCount && i < _pool.Count; i++) 
                    _currentChoices.Add(_pool[i]); 
            } 
        }

        private void Shuffle(List<RewardOption> list) 
        { 
            for (int i = list.Count - 1; i > 0; i--) 
            { 
                int j = UnityEngine.Random.Range(0, i + 1); 
                (list[i], list[j]) = (list[j], list[i]); 
            } 
        }

        //재굴림넣말
        public void OnReroll() 
        { 
            if (_rerollRemaining <= 0) return; _rerollRemaining--; PickChoices(); 
            if (GameBridge.Instance != null) GameBridge.Instance.ShowRewardUI(_currentChoices); 
        }
    }
}