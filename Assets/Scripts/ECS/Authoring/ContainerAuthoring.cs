using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Meow.ECS.Components;

namespace Meow.ECS.Authoring
{
    /// <summary>
    /// 컨테이너 Authoring (런타임 생성 방식)
    /// </summary>
    public class ContainerAuthoring : MonoBehaviour
    {
        [Header("컨테이너 설정")]
        public IngredientType providedIngredient = IngredientType.Bread;
        public bool allowReturn = true;
        public bool isInfinite = true;

        [Header("스테이션 설정")]
        public int stationID = 0;

        [Header("상호작용 설정")]
        public float interactionRange = 2.0f;

        [Header("Physics 설정")]
        public float colliderRadius = 0.5f;

        private Entity _containerEntity;
        private EntityManager _entityManager;

        private void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            // 엔티티 생성
            _containerEntity = _entityManager.CreateEntity();

            var position = transform.position;

            // Transform 추가
            _entityManager.AddComponentData(_containerEntity,
                LocalTransform.FromPosition(position));

            // StationComponent 추가
            _entityManager.AddComponentData(_containerEntity, new StationComponent
            {
                Type = StationType.Container,
                StationID = stationID,
                PlacedItemEntity = Entity.Null
            });

            // ContainerComponent 추가
            _entityManager.AddComponentData(_containerEntity, new ContainerComponent
            {
                ProvidedIngredient = providedIngredient,
                AllowReturn = allowReturn,
                IsInfinite = isInfinite
            });

            // InteractableComponent 추가
            _entityManager.AddComponentData(_containerEntity, new InteractableComponent
            {
                IsActive = true,
                InteractionRange = interactionRange
            });

            // Physics Collider 추가
            var collider = Unity.Physics.BoxCollider.Create(
                new Unity.Physics.BoxGeometry
                {
                    Center = float3.zero,
                    Orientation = quaternion.identity,
                    Size = new float3(1f, 1f, 1f),
                    BevelRadius = 0.05f
                },
                new Unity.Physics.CollisionFilter
                {
                    BelongsTo = 1u << 6,  // Layer 6 = Station
                    CollidesWith = ~0u,
                    GroupIndex = 0
                }
            );

            _entityManager.AddComponentData(_containerEntity, new PhysicsCollider
            {
                Value = collider
            });

            Debug.Log($"[ContainerAuthoring] 컨테이너 엔티티 생성 완료! ID: {stationID}, 재료: {providedIngredient}");
        }

        private void LateUpdate()
        {
            // GameObject 위치 동기화 (움직일 일은 없지만 일관성 위해)
            if (_entityManager.Exists(_containerEntity))
            {
                var lt = _entityManager.GetComponentData<LocalTransform>(_containerEntity);
                transform.position = lt.Position;
            }
        }

        private void OnDestroy()
        {
            if (World.DefaultGameObjectInjectionWorld == null ||
                !World.DefaultGameObjectInjectionWorld.IsCreated)
                return;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (em.Exists(_containerEntity))
            {
                // Physics Collider Dispose
                if (em.HasComponent<PhysicsCollider>(_containerEntity))
                {
                    var collider = em.GetComponentData<PhysicsCollider>(_containerEntity);
                    if (collider.Value.IsCreated)
                    {
                        collider.Value.Dispose();
                    }
                }

                em.DestroyEntity(_containerEntity);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 상호작용 범위
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, interactionRange);

            // 컨테이너 박스
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, Vector3.one);

            // Collider 범위
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && _entityManager != null && _entityManager.Exists(_containerEntity))
            {
                var station = _entityManager.GetComponentData<StationComponent>(_containerEntity);
                var container = _entityManager.GetComponentData<ContainerComponent>(_containerEntity);

                // 라벨 표시
                Vector3 labelPos = transform.position + Vector3.up * 2f;
                string info = $"Container\n{container.ProvidedIngredient}";

                if (station.HasItem)
                    info += "\n[아이템 있음]";

                UnityEditor.Handles.Label(labelPos, info, new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = new GUIStyleState() { textColor = Color.cyan },
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                });
            }
        }
#endif
    }
}