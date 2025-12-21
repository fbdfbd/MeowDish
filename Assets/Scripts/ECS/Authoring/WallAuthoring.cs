using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;

namespace Meow.ECS.Authoring
{
    public class WallAuthoring : MonoBehaviour
    {
        [Header("벽 설정")]
        public Vector3 wallSize = new Vector3(2f, 1f, 0.5f);

        [Tooltip("벽 중심의 높이 오프셋 (월드 기준)")]
        public float centerYOffset = 0.5f;

        [Header("물리 레이어 설정 (CollisionFilter)")]
        public int belongsToBit = 6;

        [Tooltip("CollidesWith 마스크 (기본값: 전부와 충돌)")]
        public uint collidesWithMask = ~0u;

        [Header("디버그")]
        [Tooltip("씬 뷰에서 벽 기즈모를 표시할지 여부")]
        public bool drawGizmos = true;


        private Entity _wallEntity;
        private EntityManager _entityManager;

        private BlobAssetReference<Unity.Physics.Collider> _colliderBlobRef;


        private void Start()
        {
            if (!InitializeEntityManager()) return;

            CreateWallEntity();

            SetupTransform();
            SetupPhysics();
        }

        private void OnDestroy()
        {
            DisposeWallResources();
        }


        private bool InitializeEntityManager()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogError($"[WallAuthoring:{name}] DefaultGameObjectInjectionWorld가 없습니다.");
                return false;
            }

            _entityManager = world.EntityManager;
            return true;
        }

        private void CreateWallEntity()
        {
            _wallEntity = _entityManager.CreateEntity();
#if UNITY_EDITOR
            _entityManager.SetName(_wallEntity, $"Wall_{gameObject.name}_{gameObject.GetInstanceID()}");
#endif
        }

        private void SetupTransform()
        {
            var position = transform.position + new Vector3(0f, centerYOffset, 0f);
            var rotation = transform.rotation;

            const float entityScale = 1f;

            _entityManager.AddComponentData(_wallEntity, LocalTransform.FromPositionRotationScale(
                (float3)position,
                (quaternion)rotation,
                entityScale
            ));

            _entityManager.AddComponentData(_wallEntity, new LocalToWorld
            {
                Value = float4x4.TRS(
                    (float3)position,
                    (quaternion)rotation,
                    new float3(entityScale)
                )
            });
        }

        private void SetupPhysics()
        {
            uint belongsToMask = 0u;
            if (belongsToBit >= 0 && belongsToBit < 32)
            {
                belongsToMask = 1u << belongsToBit;
            }
            else
            {
                Debug.LogWarning($"[WallAuthoring:{name}] belongsToBit 범위 오류 (0~31). 현재: {belongsToBit}");
                belongsToMask = 1u << 0; 
            }

            // 에디터 벽 크기 적용
            float3 currentScale = (float3)transform.localScale;
            float3 finalSize = (float3)wallSize * currentScale;

            var boxGeometry = new BoxGeometry
            {
                Center = float3.zero,
                Orientation = quaternion.identity,
                Size = finalSize, 
                BevelRadius = 0.0f
            };

            _colliderBlobRef = Unity.Physics.BoxCollider.Create(
                boxGeometry,
                new CollisionFilter
                {
                    BelongsTo = belongsToMask,
                    CollidesWith = collidesWithMask,
                    GroupIndex = 0
                }
            );

            _entityManager.AddComponentData(_wallEntity, new PhysicsCollider
            {
                Value = _colliderBlobRef
            });

            _entityManager.AddSharedComponent(_wallEntity, new PhysicsWorldIndex(0));

            _entityManager.AddComponentData(_wallEntity, new PhysicsVelocity());
            _entityManager.AddComponentData(_wallEntity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));

            _entityManager.AddComponent<Simulate>(_wallEntity);
        }


        private void DisposeWallResources()
        {
            if (_colliderBlobRef.IsCreated)
            {
                _colliderBlobRef.Dispose();
            }

            if (World.DefaultGameObjectInjectionWorld != null &&
                World.DefaultGameObjectInjectionWorld.IsCreated)
            {
                var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                if (em.Exists(_wallEntity))
                {
                    em.DestroyEntity(_wallEntity);
                    _wallEntity = Entity.Null;
                }
            }
        }


        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            Gizmos.color = Color.green;
            var center = transform.position + new Vector3(0f, centerYOffset, 0f);

            // 회전 적용
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.matrix = rotationMatrix;

            Vector3 finalDrawSize = Vector3.Scale(wallSize, transform.localScale);

            Gizmos.DrawWireCube(Vector3.zero, finalDrawSize);

            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}