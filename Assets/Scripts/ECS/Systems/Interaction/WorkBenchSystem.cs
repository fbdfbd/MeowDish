using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Authoring
{
    public class WorkBenchAuthoring : MonoBehaviour
    {
        [Header("작업대 설정")]
        [Tooltip("최대 아이템 개수")]
        [Range(1, 10)]
        public int MaxItems = 5;

        [Header("상호작용 설정")]
        [Tooltip("상호작용 가능 거리")]
        [Range(1f, 5f)]
        public float InteractionRange = 2f;

        [Header("스테이션 ID")]
        [Tooltip("고유 ID (0 = 자동 할당)")]
        public int StationID = 0;

        [Header("아이템 배치 설정")]
        [Tooltip("아이템 배치 높이 오프셋")]
        public float ItemHeightOffset = 1f;

        [Tooltip("아이템 간격")]
        public float ItemSpacing = 0.3f;

        // 런타임 상태 (디버그용)
        private int _currentItemCount = 0;

        private Entity _workBenchEntity;
        private EntityManager _entityManager;
        private static int _nextStationID = 100;

        private void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            _workBenchEntity = _entityManager.CreateEntity();

            _entityManager.AddComponentData(_workBenchEntity,
                LocalTransform.FromPosition(transform.position));

            _entityManager.AddComponentData(_workBenchEntity, new StationComponent
            {
                Type = StationType.WorkBench,
                StationID = StationID > 0 ? StationID : _nextStationID++,
                PlacedItemEntity = Entity.Null
            });

            _entityManager.AddComponentData(_workBenchEntity, new WorkBenchComponent
            {
                MaxItems = MaxItems,
                CurrentItemCount = 0
            });

            _entityManager.AddComponentData(_workBenchEntity, new InteractableComponent
            {
                IsActive = true,
                InteractionRange = InteractionRange
            });

            _entityManager.AddBuffer<WorkBenchItem>(_workBenchEntity);

            Debug.Log($"[WorkBenchAuthoring] Created WorkBench (ID: {StationID}, Max: {MaxItems})");
        }

        private void Update()
        {
            // 런타임 상태 업데이트 (디버그용)
            if (_entityManager != null && _entityManager.Exists(_workBenchEntity))
            {
                var workBench = _entityManager.GetComponentData<WorkBenchComponent>(_workBenchEntity);
                _currentItemCount = workBench.CurrentItemCount;
            }
        }

        private void OnDestroy()
        {
            if (_entityManager != null && _entityManager.Exists(_workBenchEntity))
            {
                _entityManager.DestroyEntity(_workBenchEntity);
            }
        }

        // 항상 보이는 기즈모
        private void OnDrawGizmos()
        {
            // 작업대 표시
            Gizmos.color = new Color(0.5f, 0.3f, 0.1f); // 갈색
            Gizmos.DrawCube(transform.position, new Vector3(2f, 0.1f, 1f));

            // 아이템 슬롯 표시
            DrawItemSlots();

            // 상태 텍스트
#if UNITY_EDITOR
            DrawWorkBenchInfo();
#endif
        }

        // 선택했을 때
        private void OnDrawGizmosSelected()
        {
            // 상호작용 범위
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, InteractionRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, InteractionRange);
        }

        private void DrawItemSlots()
        {
            Vector3 basePos = transform.position + Vector3.up * ItemHeightOffset;

            for (int i = 0; i < MaxItems; i++)
            {
                Vector3 slotPos = basePos + Vector3.right * ((i - MaxItems / 2f + 0.5f) * ItemSpacing);

                // 슬롯이 차있는지 표시 (런타임)
                if (Application.isPlaying && i < _currentItemCount)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(slotPos, Vector3.one * 0.2f);
                }
                else
                {
                    Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    Gizmos.DrawWireCube(slotPos, Vector3.one * 0.2f);
                }
            }
        }

#if UNITY_EDITOR
        private void DrawWorkBenchInfo()
        {
            Vector3 labelPos = transform.position + Vector3.up * 2f;

            string info = $"[WorkBench]\nSlots: {_currentItemCount}/{MaxItems}";

            if (Application.isPlaying)
            {
                if (_currentItemCount >= MaxItems)
                    info += "\n?? FULL";
                else if (_currentItemCount > 0)
                    info += "\n?? Has Items";
                else
                    info += "\n? Empty";
            }

            UnityEditor.Handles.Label(labelPos, info, new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState() { textColor = Color.white },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            });
        }
#endif
    }
}