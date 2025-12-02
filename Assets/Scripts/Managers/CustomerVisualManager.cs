using UnityEngine;
using UnityEngine.Pool;

namespace Meow.Managers
{
    public class CustomerVisualManager : MonoBehaviour
    {
        public static CustomerVisualManager Instance { get; private set; }

        [Header("¼Õ´Ô ÇÁ¸®ÆÕ (¿©·¯ °³¸é ·£´ý °¡´É)")]
        public GameObject customerPrefab;

        private ObjectPool<GameObject> _pool;

        private void Awake()
        {
            Instance = this;

            _pool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(customerPrefab, transform),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                defaultCapacity: 10,
                maxSize: 50
            );
        }

        public GameObject SpawnCustomer(Vector3 position, Quaternion rotation)
        {
            GameObject obj = _pool.Get();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj;
        }

        public void ReturnCustomer(GameObject obj)
        {
            if (obj != null) _pool.Release(obj);
        }
    }
}