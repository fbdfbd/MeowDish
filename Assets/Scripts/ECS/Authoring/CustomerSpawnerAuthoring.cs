using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Meow.ECS.Components;
using Meow.Run;

namespace Meow.ECS.Authoring
{
    public class CustomerSpawnerAuthoring : MonoBehaviour
    {
        [Header("스폰 기본값")]
        public float spawnInterval = 5.0f;
        public float walkSpeed = 3.0f;
        public float patience = 60.0f;
        [Tooltip("이 스테이지에서 등장할 총 고객 수")]
        public int totalCustomers = 10;
        public bool spawnOnStart = true;

        [Header("주문 가능 메뉴(기본)")]
        public List<IngredientType> possibleMenu;

        private EntityManager _em;
        private Entity _spawnerEntity;

        public Entity SpawnerEntity => _spawnerEntity;

        private void Awake()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            CreateEntityIfNeeded();
        }

        private void Start()
        {
            ApplyAuthoringDefaults();
            SetActive(spawnOnStart, resetTimer: true, resetCount: true);
        }

        private void OnDestroy()
        {
            if (_em != default && _em.Exists(_spawnerEntity))
                _em.DestroyEntity(_spawnerEntity);
        }

        public void ApplyStage(StageDefinitionSO stage)
        {
            if (!IsValid()) return;

            var data = _em.GetComponentData<CustomerSpawnerComponent>(_spawnerEntity);
            data.SpawnInterval = stage.spawnInterval;
            data.WalkSpeed = stage.customerWalkSpeed;
            data.MaxPatience = stage.customerPatience;
            data.MaxCustomersPerStage = stage.totalCustomers;
            data.Timer = 0;
            data.SpawnedCount = 0;
            data.IsActive = true;
            _em.SetComponentData(_spawnerEntity, data);

            if (_em.HasBuffer<PossibleMenuElement>(_spawnerEntity))
            {
                var buffer = _em.GetBuffer<PossibleMenuElement>(_spawnerEntity);
                buffer.Clear();
                foreach (var menu in stage.menuPool)
                    buffer.Add(new PossibleMenuElement { DishType = menu });
            }
        }

        public void SetActive(bool active, bool resetTimer = false, bool resetCount = false)
        {
            if (!IsValid()) return;
            var data = _em.GetComponentData<CustomerSpawnerComponent>(_spawnerEntity);
            data.IsActive = active;
            if (resetTimer) data.Timer = 0;
            if (resetCount) data.SpawnedCount = 0;
            _em.SetComponentData(_spawnerEntity, data);
        }


        private void ApplyAuthoringDefaults()
        {
            if (!IsValid()) return;

            var data = _em.GetComponentData<CustomerSpawnerComponent>(_spawnerEntity);
            data.SpawnInterval = spawnInterval;
            data.WalkSpeed = walkSpeed;
            data.MaxPatience = patience;
            data.MaxCustomersPerStage = totalCustomers;
            data.Timer = 0;
            data.SpawnedCount = 0;
            data.IsActive = spawnOnStart;
            _em.SetComponentData(_spawnerEntity, data);

            if (_em.HasBuffer<PossibleMenuElement>(_spawnerEntity))
            {
                var buffer = _em.GetBuffer<PossibleMenuElement>(_spawnerEntity);
                buffer.Clear();
                foreach (var menu in possibleMenu)
                    buffer.Add(new PossibleMenuElement { DishType = menu });
            }
        }

        private void CreateEntityIfNeeded()
        {
            if (_em == default) return;
            if (_em.Exists(_spawnerEntity)) return;

            _spawnerEntity = _em.CreateEntity();
            _em.AddComponentData(_spawnerEntity, LocalTransform.FromPosition(transform.position));
            _em.AddComponentData(_spawnerEntity, new CustomerSpawnerComponent
            {
                SpawnInterval = spawnInterval,
                Timer = 0f,
                WalkSpeed = walkSpeed,
                MaxPatience = patience,
                MaxCustomersPerStage = totalCustomers,
                SpawnedCount = 0,
                IsActive = spawnOnStart
            });
            _em.AddBuffer<PossibleMenuElement>(_spawnerEntity);
        }

        private bool IsValid() => _em != default && _em.Exists(_spawnerEntity);
    }
}
