using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Meow.Managers
{
    public class CustomerVisualManager : MonoBehaviour
    {
        public static CustomerVisualManager Instance { get; private set; }

        [Header("손님 프리팹")]
        public GameObject customerPrefab;

        private int _defaultCapacity = 10;
        private int _maxSize = 50;

        private ObjectPool<GameObject> _pool;

        private void Awake()
        {
            Instance = this;

            _pool = new ObjectPool<GameObject>(
                createFunc: CreateCustomer, 
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: false, // 릴리즈 중복 체크 해제
                defaultCapacity: _defaultCapacity,
                maxSize: _maxSize
            );
        }

        private void Start()
        {
            PrewarmPool();
        }

        private GameObject CreateCustomer()
        {
            return Instantiate(customerPrefab, transform);
        }

        private void PrewarmPool()
        {
            List<GameObject> tempCustomers = new List<GameObject>(_defaultCapacity);

            for (int i = 0; i < _defaultCapacity; i++)
            {
                tempCustomers.Add(_pool.Get());
            }

            foreach (var customer in tempCustomers)
            {
                _pool.Release(customer);
            }
        }

        public GameObject SpawnCustomer(Vector3 position, Quaternion rotation)
        {
            GameObject obj = _pool.Get();
            obj.transform.SetPositionAndRotation(position, rotation);
            return obj;
        }

        public void ReturnCustomer(GameObject obj)
        {
            if (obj != null) _pool.Release(obj);
        }
    }
}