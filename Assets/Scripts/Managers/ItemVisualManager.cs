using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Meow.ECS.Components;

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
            public int prewarmCount;
        }

        [Header("Prefabs & Settings")]
        public List<ItemPrefabInfo> itemPrefabs;

        private Dictionary<IngredientType, ObjectPool<GameObject>> _pools;
        private Dictionary<IngredientType, int> _prewarmCounts;

        private void Awake()
        {
            Instance = this;
            InitializePools();
        }

        private void Start()
        {
            PrewarmAllPools();
        }

        private void InitializePools()
        {
            _pools = new Dictionary<IngredientType, ObjectPool<GameObject>>();
            _prewarmCounts = new Dictionary<IngredientType, int>();

            foreach (var info in itemPrefabs)
            {
                GameObject prefab = info.prefab;
                IngredientType type = info.type;

                GameObject capturedPrefab = prefab;

                var pool = new ObjectPool<GameObject>(
                    createFunc: () => {
                        var obj = Instantiate(capturedPrefab, transform);
                        obj.SetActive(false);
                        return obj;
                    },
                    actionOnGet: (obj) => { },
                    actionOnRelease: (obj) => {
                        obj.transform.position = new Vector3(0, -1000, 0);
                        obj.SetActive(false);
                    },
                    actionOnDestroy: (obj) => Destroy(obj),
                    defaultCapacity: info.prewarmCount,
                    maxSize: 100
                );

                _pools.Add(type, pool);

                int count = info.prewarmCount > 0 ? info.prewarmCount : 10;
                _prewarmCounts.Add(type, count);
            }
        }

        private void PrewarmAllPools()
        {
            foreach (var kvp in _pools)
            {
                IngredientType type = kvp.Key;
                ObjectPool<GameObject> pool = kvp.Value;
                int count = _prewarmCounts[type];

                List<GameObject> tempItems = new List<GameObject>(count);

                for (int i = 0; i < count; i++)
                {
                    tempItems.Add(pool.Get());
                }

                foreach (var item in tempItems)
                {
                    pool.Release(item);
                }
            }
        }

        public GameObject SpawnItem(IngredientType type, Vector3 position, Quaternion rotation)
        {
            if (_pools.TryGetValue(type, out var pool))
            {
                GameObject obj = pool.Get();
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.SetActive(true);
                return obj;
            }

            Debug.LogError($"[ItemVisualManager] 등록되지 않은 타입: {type}");
            return null;
        }

        public void ReturnItem(IngredientType type, GameObject obj)
        {
            if (obj == null) return;

            if (_pools.TryGetValue(type, out var pool))
            {
                pool.Release(obj);
            }
            else
            {
                Destroy(obj);
            }
        }
    }
}