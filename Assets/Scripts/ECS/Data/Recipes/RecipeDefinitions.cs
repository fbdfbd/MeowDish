using System;
using Meow.ECS.Components;

namespace Meow.Data
{
    [Serializable]
    public struct RecipeItem
    {
        public IngredientType Type;
        public ItemState State;
        public int Amount;
    }

    // 레시피 출력 결과
    [Serializable]
    public struct RecipeOutput
    {
        public ItemType ItemType;
        public IngredientType Type;
        public ItemState State;
    }

    // 레시피 정의: 입력 N개 > 출력 1개
    [Serializable]
    public struct RecipeDefinition
    {
        public string displayName;
        public RecipeItem[] Inputs;
        public RecipeOutput Output;
        public bool OrderIndependent;
    }
}
