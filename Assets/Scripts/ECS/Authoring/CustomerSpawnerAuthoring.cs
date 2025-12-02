using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Meow.ECS.Components;
using System.Collections.Generic;
using Unity.Transforms;

namespace Meow.ECS.Authoring
{
    public class CustomerSpawnerAuthoring : MonoBehaviour
    {
        [Header("스폰 설정")]
        public float spawnInterval = 5.0f;
        public float walkSpeed = 3.0f;
        public float patience = 60.0f;

        [Header("스테이지 설정")]
        [Tooltip("이 스테이지에 등장할 총 손님 수")]
        public int totalCustomers = 10; // ?? 추가됨

        [Header("주문 가능한 메뉴")]
        public List<IngredientType> possibleMenu;

        private EntityManager _entityManager;
        private Entity _spawnerEntity;

        private void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _spawnerEntity = _entityManager.CreateEntity();

            _entityManager.AddComponentData(_spawnerEntity, LocalTransform.FromPosition(transform.position));

            _entityManager.AddComponentData(_spawnerEntity, new CustomerSpawnerComponent
            {
                SpawnInterval = spawnInterval,
                Timer = 0f,
                WalkSpeed = walkSpeed,
                MaxPatience = patience,

                // ?? 데이터 주입
                MaxCustomersPerStage = totalCustomers,
                SpawnedCount = 0
            });

            var menuBuffer = _entityManager.AddBuffer<PossibleMenuElement>(_spawnerEntity);
            foreach (var menu in possibleMenu)
            {
                menuBuffer.Add(new PossibleMenuElement { DishType = menu });
            }
        }
    }
}