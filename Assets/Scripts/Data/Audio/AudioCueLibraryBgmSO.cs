using UnityEngine;

namespace Meow.Audio
{
    public enum BgmId
    {
        None,
        Lobby,
        Stage_Burger,
        Stage_Christmas,
    }

    [System.Serializable]
    public class BgmCue
    {
        public BgmId id;
        public AudioClip clip;
    }

    [CreateAssetMenu(fileName = "BgmLibrary", menuName = "Meow/Audio/BgmLibrary")]
    public class AudioCueLibraryBgmSO : ScriptableObject
    {
        public BgmCue[] cues;

        public AudioClip GetClip(BgmId id)
        {
            foreach (var c in cues)
            {
                if (c.id == id) return c.clip;
            }
            return null;
        }
    }
}
