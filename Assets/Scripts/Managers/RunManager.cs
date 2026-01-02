using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Meow.Data;
using Meow.ECS.Components;
using Meow.Bridge;
using Meow.Managers;

namespace Meow.Run
{
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [Header("Run Data")]
        public RunDefinitionSO runDefinition;

        [Header("State (runtime)")]
        [SerializeField] private RunState state = new();
        private UpgradeLoadout _activeLoadout;

        private EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public event Action<StageDefinitionSO> OnStageStarted;
        public event Action<StageDefinitionSO, GameState> OnStageEnded;

        public IEnumerable<SkillDefinitionSO> GetOwnedSkills()
        {
            if (state.permanentBuffs != null)
                foreach (var buff in state.permanentBuffs) yield return buff;
            if (state.temporaryBuffs != null)
                foreach (var buff in state.temporaryBuffs) yield return buff;
        }

        public int GetCurrentStageIndex() => state.currentStageIndex;

        private struct BuffEffects
        {
            public int ExtraFailures;
            public float TipMultiplier;
            public float SlowSpawnMultiplier;
        }


        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializePauseEntity();

            // StartRun(runDefinition);

            // 브릿지 연결
            if (GameBridge.Instance != null)
            {
                GameBridge.Instance.OnGetOwnedSkills = GetOwnedSkills;
                GameBridge.Instance.OnRestartRequested += RetryGame;
            }
        }

        private void OnDestroy()
        {
            if (GameBridge.Instance != null)
            {
                if (GameBridge.Instance.OnGetOwnedSkills == GetOwnedSkills)
                    GameBridge.Instance.OnGetOwnedSkills = null;

                GameBridge.Instance.OnRestartRequested -= RetryGame;
            }
        }

        private void InitializePauseEntity()
        {
            var em = EntityManager;
            var query = em.CreateEntityQuery(typeof(GamePauseComponent));
            if (query.CalculateEntityCount() == 0)
            {
                var entity = em.CreateEntity(typeof(GamePauseComponent));
                em.SetComponentData(entity, new GamePauseComponent { IsPaused = false });
            }
        }


        // 재시작 로직
        public void RetryGame()
        {
            Debug.Log("[RunManager] 게임 재시작 매니저 파괴 및 씬 리로드");

            ResetGameSession();

            GameBridge.Instance?.SetPause(false, isSystemPause: true);

            if (GameBridge.Instance != null)
                GameBridge.Instance.OnRestartRequested -= RetryGame;

            Destroy(gameObject); // 중복 방지 파괴
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private bool ResetGameSession()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return false;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(typeof(GameSessionComponent));
            if (!query.TryGetSingletonEntity<GameSessionComponent>(out Entity session)) return false;

            var data = em.GetComponentData<GameSessionComponent>(session);
            data.State = GameState.Ready;
            data.MaxFailures = Mathf.Max(1, data.MaxFailures);
            data.CurrentFailures = 0;
            data.TotalCustomers = 0;
            data.ServedCustomers = 0;
            data.ProcessedCount = 0;
            data.CurrentScore = 0;
            data.ScoreMultiplier = 1f;
            data.CurrentStageLevel = 0;
            data.IsStageInitialized = false;
            em.SetComponentData(session, data);

            if (em.HasBuffer<ActiveBuffElement>(session))
            {
                em.GetBuffer<ActiveBuffElement>(session).Clear();
            }

            return true;
        }


        // 게임 진행 로직
        public void StartRun(RunDefinitionSO def, UpgradeLoadout loadout = default, int startStageIndex = 0)
        {
            if (def == null) return;
            if (startStageIndex < 0 || (def.stages != null && startStageIndex >= def.stages.Count))
            {
                Debug.LogError($"[RunManager] 잘못된 startStageIndex {startStageIndex}");
                return;
            }

            ResetGameSession();

            runDefinition = def;
            _activeLoadout = loadout;
            state.Reset();
            state.permanentBuffs.AddRange(def.startingBuffs);
            if (_activeLoadout.resolvedSkills != null)
                state.permanentBuffs.AddRange(_activeLoadout.resolvedSkills);
            ApplyStartingEquipment(def.startingEquipment);
            // loadout 적용 지점?

            // 스킬 선택부터
            state.currentStageIndex = startStageIndex - 1;

            Debug.Log($"[RunManager] 시작 스킬선택");

            if (def.stages.Count > 0)
            {
                var firstStage = def.stages[startStageIndex];
                var rewardController = FindAnyObjectByType<StageRewardController>();
                if (rewardController != null)
                {
                    rewardController.TriggerRewardForStage(firstStage);
                }
                else
                {

                    NextStage();
                }
            }
        }

        public void StartStage(int index)
        {
            if (runDefinition == null || index < 0 || index >= runDefinition.stages.Count)
            {
                Debug.LogError("[RunManager] 스테이지 없음");
                return;
            }

            var stage = runDefinition.stages[index];
            AudioManager.Instance?.PlayBgm(stage.bgmId);
            state.currentStageIndex = index;

            // ECS 적용
            var buffEffects = CalculateBuffEffects(stage);
            int adjustedTotalCustomers = GetAdjustedTotalCustomers(stage);

            if (!ApplyStageToGameSession(stage, adjustedTotalCustomers, buffEffects))
            {
                Debug.LogError("[RunManager] GameSessionComponent Missing!");
                return;
            }
            Debug.Log($"[RunManager] StartStage index={index}, stageName={stage.name}, stageLevel={stage.stageLevel}");
            ApplyStageToSpawners(stage, adjustedTotalCustomers, buffEffects);
            ApplyStageBuffs(stage);

            OnStageStarted?.Invoke(stage);
        }

        public void HandleStageEnd(GameState result, int stageScoreDelta, int failuresDelta)
        {
            var stage = runDefinition.stages[state.currentStageIndex];
            state.totalScore = stageScoreDelta;
            state.totalFailures = failuresDelta;
            OnStageEnded?.Invoke(stage, result);

            SetSpawnerActive(false);

            if (result == GameState.StageClear)
            {
                Debug.Log("[RunManager] Stage Clear! Playing Effect...");

                int nextIndex = state.currentStageIndex + 1;
                bool hasNextStage = (runDefinition != null && nextIndex < runDefinition.stages.Count);

                // 다음 스테이지가 있으면 다음 스테이지 보상 > 선택 끝나면 NextStage()
                if (hasNextStage)
                {
                    var nextStage = runDefinition.stages[nextIndex];

                    if (GameBridge.Instance != null)
                    {
                        GameBridge.Instance.PlayClearEffect(() =>
                        {
                            var rewardController = FindAnyObjectByType<StageRewardController>();
                            if (rewardController != null)
                            {
                                rewardController.TriggerRewardForStage(nextStage);
                            }
                            else
                            {
                                NextStage();
                            }
                        });
                    }
                    else
                    {
                        FindAnyObjectByType<StageRewardController>()?.TriggerRewardForStage(nextStage);
                    }
                }
                // 마지막 스테이지면 보상 없이 바로 결과
                else
                {
                    if (GameBridge.Instance != null)
                    {
                        GameBridge.Instance.PlayClearEffect(() =>
                        {
                            // 결과패널에서 정지
                            GameBridge.Instance.SetPause(true, isSystemPause: true);
                            GameBridge.Instance.TriggerGameResult(true, state.totalScore);
                        });
                    }
                    else
                    {
                        Debug.LogWarning("[RunManager] GameBridge 없음 End불가");
                    }
                }
            }
            else if (result == GameState.GameOver)
            {
                Debug.Log("[RunManager] 게임 오버");

                if (GameBridge.Instance != null)
                {
                    GameBridge.Instance.SetPause(true, isSystemPause: true);
                    GameBridge.Instance.TriggerGameResult(false, state.totalScore);
                }
            }
        }

        public void NextStage()
        {
            int next = state.currentStageIndex + 1;
            Debug.Log($"[RunManager] NextStage 요청됨 -> {next}");

            if (runDefinition != null && next < runDefinition.stages.Count)
            {
                StartStage(next);
            }
            else
            {
                Debug.Log("[RunManager] 모든스테이지 끝");
                if (GameBridge.Instance != null)
                    GameBridge.Instance.TriggerGameResult(true, state.totalScore);
            }
        }



        public void GrantReward(SkillDefinitionSO skill)
        {
            if (skill == null) return;
            if (skill.isPermanent) state.permanentBuffs.Add(skill);
            else state.temporaryBuffs.Add(skill);
        }

        public void GrantReward(EquipmentItemSO equip)
        {
            if (equip == null) return;
            Equip(equip);
        }

        private BuffEffects CalculateBuffEffects(StageDefinitionSO stage)
        {
            var effects = new BuffEffects { ExtraFailures = 0, TipMultiplier = 1f, SlowSpawnMultiplier = 1f };
            foreach (var skill in EnumerateActiveSkills(stage))
            {
                if (skill == null) continue;
                switch (skill.buffType)
                {
                    case BuffType.ExtraLife:
                        int lives = (skill.valueOp == EffectOp.Add && skill.flatValue != 0) ? skill.flatValue : Mathf.RoundToInt(skill.multiplier);
                        effects.ExtraFailures += lives;
                        break;
                    case BuffType.TipBoost:
                        if (skill.valueOp == EffectOp.Add) effects.TipMultiplier *= 1f + Mathf.Max(0f, skill.multiplier);
                        else effects.TipMultiplier *= Mathf.Max(0f, skill.multiplier);
                        break;
                    case BuffType.SlowSpawn:
                        if (skill.valueOp == EffectOp.Add) effects.SlowSpawnMultiplier *= 1f + Mathf.Max(0f, skill.multiplier);
                        else effects.SlowSpawnMultiplier *= Mathf.Max(0f, skill.multiplier);
                        break;
                }
            }
            if (effects.TipMultiplier <= 0f) effects.TipMultiplier = 1f;
            if (effects.SlowSpawnMultiplier <= 0f) effects.SlowSpawnMultiplier = 1f;
            return effects;
        }

        private void ApplyStartingEquipment(List<EquipmentItemSO> items)
        {
            if (items == null) return;
            foreach (var item in items) Equip(item);
        }

        private int GetAdjustedTotalCustomers(StageDefinitionSO stage)
        {
            int extra = CalculateAdditionalCustomers(stage);
            extra += _activeLoadout.additionalCustomers;
            return Mathf.Max(0, stage.totalCustomers + extra);
        }

        private int CalculateAdditionalCustomers(StageDefinitionSO stage)
        {
            int extra = 0;
            foreach (var skill in EnumerateActiveSkills(stage)) extra += skill.additionalCustomers;
            return extra;
        }

        private IEnumerable<SkillDefinitionSO> EnumerateActiveSkills(StageDefinitionSO stage)
        {
            foreach (var skill in state.permanentBuffs) if (skill != null) yield return skill;
            foreach (var skill in state.temporaryBuffs) if (skill != null) yield return skill;
            if (stage?.startingBuffs != null) foreach (var skill in stage.startingBuffs) if (skill != null) yield return skill;
        }

        private void Equip(EquipmentItemSO item)
        {
            switch (item.slot)
            {
                case EquipmentSlot.Head: state.equipmentSlots.HeadEquipmentID = item.GetInstanceID(); break;
                case EquipmentSlot.Body: state.equipmentSlots.BodyEquipmentID = item.GetInstanceID(); break;
                case EquipmentSlot.Hands: state.equipmentSlots.HandsEquipmentID = item.GetInstanceID(); break;
                case EquipmentSlot.Feet: state.equipmentSlots.FeetEquipmentID = item.GetInstanceID(); break;
                case EquipmentSlot.Accessory1: state.equipmentSlots.Accessory1EquipmentID = item.GetInstanceID(); break;
                case EquipmentSlot.Accessory2: state.equipmentSlots.Accessory2EquipmentID = item.GetInstanceID(); break;
            }
        }

        private bool ApplyStageToGameSession(StageDefinitionSO stage, int totalCustomers, BuffEffects effects)
        {
            var em = EntityManager;
            var query = em.CreateEntityQuery(typeof(GameSessionComponent));
            if (!query.TryGetSingletonEntity<GameSessionComponent>(out Entity session)) return false;

            float stageScoreMult = (stage.scoreMultiplier <= 0f) ? 1f : stage.scoreMultiplier;
            float loadoutScoreMult = (_activeLoadout.scoreMultiplierBonus <= 0f) ? 1f : _activeLoadout.scoreMultiplierBonus;

            var data = em.GetComponentData<GameSessionComponent>(session);
            data.State = GameState.Playing;
            data.MaxFailures = Mathf.Max(1, stage.maxFailures + effects.ExtraFailures + _activeLoadout.extraLives);
            data.CurrentFailures = Mathf.Min(state.totalFailures, data.MaxFailures);
            data.TotalCustomers = totalCustomers;
            data.ServedCustomers = 0;
            data.ProcessedCount = 0;
            data.CurrentScore = state.totalScore + _activeLoadout.bonusGold;
            data.ScoreMultiplier = effects.TipMultiplier * stageScoreMult * loadoutScoreMult;
            data.CurrentStageLevel = stage.stageLevel;
            data.IsStageInitialized = false;

            em.SetComponentData(session, data);
            return true;
        }

        private void ApplyStageToSpawners(StageDefinitionSO stage, int totalCustomers, BuffEffects effects)
        {
            var em = EntityManager;
            using var spawners = em.CreateEntityQuery(typeof(CustomerSpawnerComponent)).ToEntityArray(Allocator.Temp);
            foreach (var spawner in spawners)
            {
                var data = em.GetComponentData<CustomerSpawnerComponent>(spawner);
                float spawnMult = (_activeLoadout.spawnIntervalMultiplier <= 0f) ? 1f : _activeLoadout.spawnIntervalMultiplier;
                float patienceMult = (_activeLoadout.patienceMultiplier <= 0f) ? 1f : _activeLoadout.patienceMultiplier;

                float spawnInterval = stage.spawnInterval * (effects.SlowSpawnMultiplier > 0f ? effects.SlowSpawnMultiplier : 1f);
                spawnInterval *= spawnMult;
                data.SpawnInterval = Mathf.Max(0.01f, spawnInterval);
                data.Timer = 0;
                data.WalkSpeed = stage.customerWalkSpeed;
                data.MaxPatience = stage.customerPatience * patienceMult;
                data.MaxCustomersPerStage = totalCustomers;
                data.SpawnedCount = 0;
                data.IsActive = true;
                em.SetComponentData(spawner, data);

                if (em.HasBuffer<PossibleMenuElement>(spawner))
                {
                    var buffer = em.GetBuffer<PossibleMenuElement>(spawner);
                    buffer.Clear();
                    foreach (var menu in stage.menuPool) buffer.Add(new PossibleMenuElement { DishType = menu });
                }
                if (em.HasComponent<ServingStationComponent>(spawner))
                {
                    var station = em.GetComponentData<ServingStationComponent>(spawner);
                    station.MaxQueueCapacity = stage.maxQueueCapacity;
                    em.SetComponentData(spawner, station);
                }
            }
        }

        private void ApplyStageBuffs(StageDefinitionSO stage)
        {
            var em = EntityManager;
            var query = em.CreateEntityQuery(typeof(GameSessionComponent));
            if (!query.TryGetSingletonEntity<GameSessionComponent>(out Entity session)) return;

            if (em.HasBuffer<ActiveBuffElement>(session))
            {
                var buffer = em.GetBuffer<ActiveBuffElement>(session);
                buffer.Clear();
                foreach (var buff in EnumerateActiveSkills(stage)) buffer.Add(new ActiveBuffElement { Type = buff.buffType });
            }
        }

        private void SetSpawnerActive(bool active)
        {
            var em = EntityManager;
            using var spawners = em.CreateEntityQuery(typeof(CustomerSpawnerComponent)).ToEntityArray(Allocator.Temp);
            foreach (var spawner in spawners)
            {
                var data = em.GetComponentData<CustomerSpawnerComponent>(spawner);
                data.IsActive = active;
                em.SetComponentData(spawner, data);
            }
        }
    }
}