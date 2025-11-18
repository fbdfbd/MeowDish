using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Authoring
{
    public class ContainerAuthoring : MonoBehaviour
    {
        [Header("컨테이너 설정")]
        [Tooltip("제공하는 재료 타입")]
        public IngredientType ProvidedIngredient = IngredientType.Bread;

        [Tooltip("재료를 다시 넣을 수 있는지")]
        public bool AllowReturn = true;

        [Tooltip("무한 제공 여부")]
        public bool IsInfinite = true;

        [Header("상호작용 설정")]
        [Tooltip("상호작용 가능 거리")]
        [Range(1f, 5f)]
        public float InteractionRange = 2f;

        [Header("스테이션 ID")]
        [Tooltip("고유 ID (0 = 자동 할당)")]
        public int StationID = 0;

        [Header("시각 효과")]
        [Tooltip("아이콘 크기")]
        public float IconSize = 0.5f;

        private Entity _containerEntity;
        private EntityManager _entityManager;
        private static int _nextStationID = 1;

        private void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            _containerEntity = _entityManager.CreateEntity();

            _entityManager.AddComponentData(_containerEntity,
                LocalTransform.FromPosition(transform.position));

            _entityManager.AddComponentData(_containerEntity, new StationComponent
            {
                Type = StationType.Container,
                StationID = StationID > 0 ? StationID : _nextStationID++,
                PlacedItemEntity = Entity.Null
            });

            _entityManager.AddComponentData(_containerEntity, new ContainerComponent
            {
                ProvidedIngredient = ProvidedIngredient,
                AllowReturn = AllowReturn,
                IsInfinite = IsInfinite
            });

            _entityManager.AddComponentData(_containerEntity, new InteractableComponent
            {
                IsActive = true,
                InteractionRange = InteractionRange
            });

            Debug.Log($"[ContainerAuthoring] Created {ProvidedIngredient} container (ID: {StationID})");
        }

        private void OnDestroy()
        {
            if (_entityManager != null && _entityManager.Exists(_containerEntity))
            {
                _entityManager.DestroyEntity(_containerEntity);
            }
        }

        // 항상 보이는 기즈모
        private void OnDrawGizmos()
        {
            // 컨테이너 박스
            Gizmos.color = GetIngredientColor();
            Gizmos.DrawCube(transform.position, Vector3.one * 0.8f);

            // 재료 타입 아이콘
            DrawIngredientIcon();

            // 상태 텍스트 (Scene 뷰에만)
#if UNITY_EDITOR
            DrawStationInfo();
#endif
        }

        // 선택했을 때 추가 기즈모
        private void OnDrawGizmosSelected()
        {
            // 상호작용 범위
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, InteractionRange);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, InteractionRange);
        }

        private Color GetIngredientColor()
        {
            return ProvidedIngredient switch
            {
                IngredientType.Bread => new Color(0.9f, 0.7f, 0.4f), // 갈색
                IngredientType.Meat => new Color(0.8f, 0.2f, 0.2f),  // 빨간색
                IngredientType.Lettuce => new Color(0.2f, 0.8f, 0.2f), // 초록색
                IngredientType.Tomato => new Color(1f, 0.3f, 0.3f),   // 토마토색
                IngredientType.Cheese => new Color(1f, 0.9f, 0.3f),   // 노란색
                IngredientType.Plate => new Color(0.9f, 0.9f, 0.9f),  // 흰색
                _ => Color.gray
            };
        }

        private void DrawIngredientIcon()
        {
            Vector3 iconPos = transform.position + Vector3.up * 1.5f;

            switch (ProvidedIngredient)
            {
                case IngredientType.Bread:
                    // 빵 모양 (사각형)
                    Gizmos.color = new Color(0.9f, 0.7f, 0.4f);
                    Gizmos.DrawCube(iconPos, Vector3.one * IconSize);
                    break;

                case IngredientType.Meat:
                    // 고기 모양 (구)
                    Gizmos.color = new Color(0.8f, 0.2f, 0.2f);
                    Gizmos.DrawSphere(iconPos, IconSize * 0.5f);
                    break;

                case IngredientType.Lettuce:
                    // 양상추 (납작한 원)
                    Gizmos.color = new Color(0.2f, 0.8f, 0.2f);
                    Matrix4x4 oldMatrix = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(iconPos, Quaternion.identity, new Vector3(1, 0.3f, 1));
                    Gizmos.DrawSphere(Vector3.zero, IconSize * 0.5f);
                    Gizmos.matrix = oldMatrix;
                    break;

                case IngredientType.Tomato:
                    // 토마토 (구)
                    Gizmos.color = new Color(1f, 0.3f, 0.3f);
                    Gizmos.DrawSphere(iconPos, IconSize * 0.4f);
                    break;

                case IngredientType.Cheese:
                    // 치즈 (납작한 큐브)
                    Gizmos.color = new Color(1f, 0.9f, 0.3f);
                    Gizmos.DrawCube(iconPos, new Vector3(IconSize, IconSize * 0.3f, IconSize));
                    break;

                case IngredientType.Plate:
                    // 접시 (원판)
                    Gizmos.color = Color.white;
                    Matrix4x4 oldMatrix2 = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(iconPos, Quaternion.identity, new Vector3(1, 0.1f, 1));
                    Gizmos.DrawSphere(Vector3.zero, IconSize * 0.6f);
                    Gizmos.matrix = oldMatrix2;
                    break;
            }
        }

#if UNITY_EDITOR
        private void DrawStationInfo()
        {
            Vector3 labelPos = transform.position + Vector3.up * 2.5f;

            string info = $"[Container]\n{ProvidedIngredient}";
            if (IsInfinite) info += "\n?? Infinite";
            if (AllowReturn) info += "\n?? Return OK";

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