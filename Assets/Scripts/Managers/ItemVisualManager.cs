using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool; // 유니티 내장 풀링 사용
using Meow.ECS.Components; // IngredientType Enum 사용

namespace Meow.Managers
{
    public class ItemVisualManager : MonoBehaviour
    {
        public static ItemVisualManager Instance { get; private set; }

        [System.Serializable]
        public struct ItemPrefabInfo
        {
            public IngredientType type;
            public GameObject prefab;
        }

        [Header("Prefabs")]
        public List<ItemPrefabInfo> itemPrefabs;

        // 타입별 풀(Pool) 저장소
        private Dictionary<IngredientType, ObjectPool<GameObject>> _pools;

        private void Awake()
        {
            Instance = this;
            InitializePools();
        }

        private void InitializePools()
        {
            _pools = new Dictionary<IngredientType, ObjectPool<GameObject>>();

            foreach (var info in itemPrefabs)
            {
                // 람다 캡처 문제를 피하기 위해 로컬 변수 복사
                GameObject prefab = info.prefab;

                var pool = new ObjectPool<GameObject>(
                    createFunc: () => Instantiate(prefab),
                    actionOnGet: (obj) => obj.SetActive(true),
                    actionOnRelease: (obj) => obj.SetActive(false),
                    actionOnDestroy: (obj) => Destroy(obj),
                    defaultCapacity: 10,
                    maxSize: 50
                );

                _pools.Add(info.type, pool);
            }
        }

        /// <summary>
        /// 풀에서 아이템 하나 꺼내오기
        /// </summary>
        public GameObject SpawnItem(IngredientType type, Vector3 position, Quaternion rotation)
        {
            if (_pools.TryGetValue(type, out var pool))
            {
                GameObject obj = pool.Get();
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                return obj;
            }

            Debug.LogError($"[ItemVisualManager] 프리팹이 등록되지 않은 타입입니다: {type}");
            return null;
        }

        /// <summary>
        /// 풀에 아이템 반납하기
        /// </summary>
        public void ReturnItem(IngredientType type, GameObject obj)
        {
            if (obj == null) return;

            if (_pools.TryGetValue(type, out var pool))
            {
                pool.Release(obj);
            }
            else
            {
                // 풀이 없으면 그냥 파괴 (안전장치)
                Destroy(obj);
            }
        }
    }
}