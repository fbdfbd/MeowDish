using UnityEngine;
using Meow.Data;
using Meow.Run;

namespace Meow.Bootstrap
{
    public class RunEntryContext
    {
        public RunDefinitionSO run;
        public int startStageIndex;
        public UpgradeLoadout loadout; // 나중에 업그레이드/장비/버프 묶음
    }

    public class RunBootstrap : MonoBehaviour
    {
        public static RunBootstrap Instance { get; private set; }

        private RunEntryContext _context;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Register(RunEntryContext ctx)
        {
            _context = ctx;
        }

        public bool TryGet(out RunEntryContext ctx)
        {
            ctx = _context;
            return ctx != null;
        }

        public void Clear()
        {
            _context = null;
        }
    }
}
