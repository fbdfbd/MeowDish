using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Meow.Data;
using Meow.ECS.Components;

namespace Meow.ECS.Data.Recipes
{
    public static class RecipeBlobBuilder
    {
        public static BlobAssetReference<RecipeBlob> Build(RecipeDefinition[] defs)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<RecipeBlob>();

            if (defs == null || defs.Length == 0)
            {
                builder.Allocate(ref root.Entries, 0);

                var emptyRef = builder.CreateBlobAssetReference<RecipeBlob>(Allocator.Persistent);
                builder.Dispose();
                return emptyRef;
            }

            var entryArray = builder.Allocate(ref root.Entries, defs.Length);

            for (int i = 0; i < defs.Length; i++)
            {
                var def = defs[i];

                var keys = CreateInputKeys(def);

                if (def.OrderIndependent && keys.Length > 1)
                {
                    keys.AsArray().Sort();
                }

                ref var entry = ref entryArray[i];

                entry.OrderIndependent = (byte)(def.OrderIndependent ? 1 : 0);

                entry.OutputItemType = (byte)def.Output.ItemType;

                var inputBlob = builder.Allocate(ref entry.Inputs, keys.Length);
                for (int k = 0; k < keys.Length; k++)
                    inputBlob[k] = keys[k];

                entry.OutputKey = PackKey(def.Output.Type, def.Output.State);

                keys.Dispose();
            }

            var blobRef = builder.CreateBlobAssetReference<RecipeBlob>(Allocator.Persistent);
            builder.Dispose();
            return blobRef;
        }

        private static NativeList<ushort> CreateInputKeys(RecipeDefinition def)
        {
            var keys = new NativeList<ushort>(Allocator.Temp);

            if (def.Inputs != null)
            {
                for (int i = 0; i < def.Inputs.Length; i++)
                {
                    var item = def.Inputs[i];
                    ushort key = PackKey(item.Type, item.State);

                    int count = math.max(1, item.Amount);
                    for (int c = 0; c < count; c++)
                        keys.Add(key);
                }
            }

            return keys;
        }

        public static ushort PackKey(IngredientType type, ItemState state)
        {
            return (ushort)(((int)type << 8) | ((int)state & 0xFF));
        }
    }

    public struct RecipeBlob
    {
        public BlobArray<RecipeEntryBlob> Entries;
    }

    public struct RecipeEntryBlob
    {
        public BlobArray<ushort> Inputs;
        public ushort OutputKey;
        public byte OutputItemType;
        public byte OrderIndependent;     // 1 = 순서 무시
    }

    public struct RecipeLookup : IComponentData
    {
        public BlobAssetReference<RecipeBlob> Blob;
    }
}
