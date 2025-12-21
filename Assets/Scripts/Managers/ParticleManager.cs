using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Meow.Managers
{
    public enum ParticleType
    {
        StoveSmoke = 0,
        ServingSuccess = 1,
    }

    [System.Serializable]
    public struct ParticlePrefabInfo
    {
        public ParticleType type;
        public GameObject prefab;
        [Min(0)] public int prewarmCount;
    }

    public class ParticleManager : MonoBehaviour
    {
        public static ParticleManager Instance { get; private set; }

        [Header("Prefabs & Pool Settings")]
        public List<ParticlePrefabInfo> particlePrefabs = new();

        private readonly Dictionary<ParticleType, ObjectPool<GameObject>> _pools = new();
        private readonly Dictionary<ParticleType, int> _prewarmCounts = new();
        private readonly Dictionary<int, ActiveInstance> _activeLoops = new();

        private int _nextHandle = 1;

        private class ActiveInstance
        {
            public ParticleType Type;
            public GameObject Obj;
            public Transform Follow;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildPools();
        }

        private void Start()
        {
            PrewarmAll();
        }

        private void BuildPools()
        {
            _pools.Clear();
            _prewarmCounts.Clear();

            foreach (var info in particlePrefabs)
            {
                if (info.prefab == null) continue;

                var capturedPrefab = info.prefab;
                int prewarm = Mathf.Max(1, info.prewarmCount);
                Vector3 offscreenPos = new Vector3(0f, -1000f, 0f);

                var pool = new ObjectPool<GameObject>(
                    createFunc: () => {
                        var obj = Instantiate(capturedPrefab, transform);
                        obj.SetActive(false);
                        return obj;
                    },
                    actionOnGet: (obj) => { }, // Get 시 활성화는 Spawn 메서드에서 처리
                    actionOnRelease: (obj) => {
                        obj.transform.SetPositionAndRotation(offscreenPos, Quaternion.identity);
                        obj.SetActive(false);
                    },
                    actionOnDestroy: (obj) => Destroy(obj),
                    defaultCapacity: prewarm,
                    maxSize: 64
                );

                _pools[info.type] = pool;
                _prewarmCounts[info.type] = prewarm;
            }
        }

        private void PrewarmAll()
        {
            foreach (var kvp in _pools)
            {
                var type = kvp.Key;
                var pool = kvp.Value;
                int count = _prewarmCounts[type];

                var temp = new List<GameObject>(count);
                for (int i = 0; i < count; i++) temp.Add(pool.Get());
                foreach (var obj in temp) pool.Release(obj);
            }
        }


        // 루프형 파티클 스폰(예: 스토브 연기) handle 반환 DespawnLoop로 종료
        public int SpawnLoop(ParticleType type, Vector3 position, Quaternion rotation = default, Transform follow = null)
        {
            if (!_pools.TryGetValue(type, out var pool))
            {
                Debug.LogWarning($"[ParticleManager] Pool not found for {type}");
                return 0;
            }

            Quaternion rot = rotation == default ? Quaternion.identity : rotation;

            var obj = pool.Get();
            obj.transform.SetPositionAndRotation(position, rot);
            obj.SetActive(true);

            var ps = obj.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play(true);

            int handle = _nextHandle++;
            _activeLoops[handle] = new ActiveInstance
            {
                Type = type,
                Obj = obj,
                Follow = follow
            };

            return handle;
        }

        // 루프형 파티클 종료/반납
        public void DespawnLoop(int handle)
        {
            if (!_activeLoops.TryGetValue(handle, out var inst)) return;
            _activeLoops.Remove(handle);

            if (inst.Obj == null) return;

            if (_pools.TryGetValue(inst.Type, out var pool))
            {
                pool.Release(inst.Obj);
            }
            else
            {
                Destroy(inst.Obj);
            }
        }

        // 한 번만 재생 후 자동 반환
        public void SpawnOneShot(ParticleType type, Vector3 position, Quaternion rotation = default)
        {
            if (!_pools.TryGetValue(type, out var pool))
            {
                Debug.LogWarning($"[ParticleManager] Pool not found for {type}");
                return;
            }

            Quaternion rot = rotation == default ? Quaternion.identity : rotation;

            var obj = pool.Get();
            obj.transform.SetPositionAndRotation(position, rot);
            obj.SetActive(true);

            var ps = obj.GetComponent<ParticleSystem>();
            float lifetime = 1.5f;

            if (ps != null)
            {
                ps.Play(true);
                var main = ps.main;
                lifetime = main.duration + main.startLifetime.constantMax;
                if (main.loop) lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
            }

            StartCoroutine(CoReturnAfter(type, obj, lifetime));
        }


        private void LateUpdate()
        {
            if (_activeLoops.Count == 0) return;
            foreach (var inst in _activeLoops.Values)
            {
                if (inst.Follow != null && inst.Obj != null)
                {
                    inst.Obj.transform.position = inst.Follow.position;
                }
            }
        }

        private System.Collections.IEnumerator CoReturnAfter(ParticleType type, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj == null) yield break;

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
