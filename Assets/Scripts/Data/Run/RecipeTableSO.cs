using UnityEngine;

namespace Meow.Data
{
    [CreateAssetMenu(fileName = "RecipeTable", menuName = "Meow/Recipes/RecipeTable")]
    public class RecipeTableSO : ScriptableObject
    {
        [Tooltip("OrderIndependent = ture : 순서 무시")]
        public RecipeDefinition[] recipes;
    }
}
