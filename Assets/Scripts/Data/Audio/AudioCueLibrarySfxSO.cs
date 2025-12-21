using UnityEngine;

namespace Meow.Audio
{
    public enum SfxId
    {
        Pickup,
        Drop,
        Grilling,
        Burned,
        ServeSuccess,
        ServeFail,
        Footstep,
        UIConfirm,
        Alert,
        Clear,
        Click,
        Cutting,
        ClockTick,
        Coins,
        Pause,
        Plate,
        SkillSelect,
        Swing,
        Up,
        Down,
        Unpause,
        Meow
    }

    [System.Serializable]
    public class SfxCue
    {
        public SfxId id;
        [Tooltip("Loop? (예: Grilling)")]
        public bool loop = false;
        public AudioClip[] clips;
    }

    [CreateAssetMenu(fileName = "SfxLibrary", menuName = "Meow/Audio/SfxLibrary")]
    public class AudioCueLibrarySfxSO : ScriptableObject
    {
        public SfxCue[] cues;

        public SfxCue GetCue(SfxId id)
        {
            foreach (var c in cues)
            {
                if (c.id == id) return c;
            }
            return null;
        }

        public AudioClip GetRandomClip(SfxId id)
        {
            foreach (var c in cues)
            {
                if (c.id != id) continue;
                if (c.clips == null || c.clips.Length == 0) return null;
                int idx = UnityEngine.Random.Range(0, c.clips.Length);
                return c.clips[idx];
            }
            return null;
        }

        public AudioClip GetClipByIndex(SfxId id, int index)
        {
            foreach (var c in cues)
            {
                if (c.id != id) continue;
                if (c.clips == null || c.clips.Length == 0) return null;
                int clamped = Mathf.Clamp(index, 0, c.clips.Length - 1);
                return c.clips[clamped];
            }
            return null;
        }
    }
}
