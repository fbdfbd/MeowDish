using System.Collections.Generic;
using UnityEngine;

namespace Meow.Run
{
    [CreateAssetMenu(fileName = "StagePresetCatalog", menuName = "Meow/StagePresetCatalog")]
    public class StagePresetCatalogSO : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public string stageKey;
            public StagePresetSO preset;
        }

        public List<Entry> entries = new();
        private Dictionary<string, StagePresetSO> _map;

        private void OnEnable() => BuildMap();

        public void BuildMap()
        {
            _map = new Dictionary<string, StagePresetSO>();
            foreach (var e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.stageKey) || e.preset == null) continue;
                _map[e.stageKey] = e.preset;
            }
        }

        public bool TryGet(string key, out StagePresetSO preset)
        {
            if (_map == null) BuildMap();
            return _map.TryGetValue(key, out preset);
        }
    }
}
