using UnityEngine;
using System.Collections.Generic;
using Meow.ECS.Components;

namespace Meow.Data
{
    [CreateAssetMenu(fileName = "ItemIconData_", menuName = "Meow/Item Icons")]
    public class ItemIconSO : ScriptableObject
    {
        [System.Serializable]
        public struct IconData
        {
            public IngredientType type;
            public Sprite icon;
        }

        public List<IconData> icons;

        // 검색용 딕셔너리
        private Dictionary<IngredientType, Sprite> _iconDict;

        public void Initialize()
        {
            _iconDict = new Dictionary<IngredientType, Sprite>();
            foreach (var item in icons)
            {
                if (!_iconDict.ContainsKey(item.type))
                    _iconDict.Add(item.type, item.icon);
            }
        }

        public Sprite GetSprite(IngredientType type)
        {
            if (_iconDict == null) Initialize();

            if (_iconDict.TryGetValue(type, out var sprite))
                return sprite;

            return null; // 없으면 투명
        }
    }
}