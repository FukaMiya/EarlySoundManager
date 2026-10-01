using UnityEngine;
using UnityEngine.Audio;

namespace Early.SoundManager
{
    [CreateAssetMenu(fileName = "SoundRegistry", menuName = "SoundManager/SoundRegistry")]
    public sealed class SoundRegistry : ScriptableObject
    {
        public SoundEntry[] SoundEntries;
        public AudioMixerGroup DefaultSeMixerGroup;
        public AudioMixerGroup DefaultBgmMixerGroup;
    }

    [System.Serializable]
    public sealed class SoundEntry
    {
        public string key;
        public AudioClip clip;
    }
}