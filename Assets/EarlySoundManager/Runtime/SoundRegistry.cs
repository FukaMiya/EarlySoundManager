using UnityEngine;

namespace Early.SoundManager
{
    [CreateAssetMenu(fileName = "SoundRegistry", menuName = "SoundManager/SoundRegistry")]
    public sealed class SoundRegistry : ScriptableObject
    {
        public SoundEntry[] SoundEntries;
    }

    [System.Serializable]
    public sealed class SoundEntry
    {
        public string key;
        public AudioClip clip;
    }
}