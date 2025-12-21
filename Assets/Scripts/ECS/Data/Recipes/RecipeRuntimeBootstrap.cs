using Unity.Entities;
using UnityEngine;
using Meow.Data;

namespace Meow.ECS.Data.Recipes
{
    public class RecipeRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private RecipeTableSO table;

        private BlobAssetReference<RecipeBlob> _blob;
        private Entity _lookupEntity;

        private void Awake()
        {
            if (table == null) return;

            // Blob 빌드
            _blob = RecipeBlobBuilder.Build(table.recipes);

            // 엔티티 생성
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _lookupEntity = em.CreateEntity(typeof(RecipeLookup));
            em.SetComponentData(_lookupEntity, new RecipeLookup { Blob = _blob });
        }

        private void OnDestroy()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                var em = world.EntityManager;
                if (_lookupEntity != Entity.Null && em.Exists(_lookupEntity))
                {
                    em.DestroyEntity(_lookupEntity);
                }
            }

            if (_blob.IsCreated) _blob.Dispose();
        }
    }
}
