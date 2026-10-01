using UnityEngine;
using UnityEngine.Audio;

namespace Early.SoundManager
{
    public interface ISoundOptions
    {
        float BaseVolume { get; }
        float BasePitch { get; }
        bool Spatialize { get; }
        Vector3 Position { get; }
        Transform PositionSource { get; }
        AudioRolloffMode RolloffMode { get; }
        float MinDistance { get; }
        float MaxDistance { get; }
        AudioMixerGroup MixerGroup { get; }
    }

    public readonly struct SoundOptions : ISoundOptions
    {
        public readonly float BaseVolume { get; }
        public readonly float BasePitch { get; }
        public readonly bool Spatialize { get; }
        public readonly Vector3 Position { get; }
        public readonly Transform PositionSource { get; }
        public readonly AudioRolloffMode RolloffMode { get; }
        public readonly float MinDistance { get; }
        public readonly float MaxDistance { get; }
        public readonly AudioMixerGroup MixerGroup { get; }

        public SoundOptions(
            float volume = 1.0f,
            float pitch = 1.0f,
            bool spatialize = false,
            Vector3 position = default,
            Transform positionSource = null,
            AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic,
            float minDistance = 1.0f,
            float maxDistance = 500.0f,
            AudioMixerGroup mixerGroup = null
        )
        {
            BaseVolume = volume;
            BasePitch = pitch;
            Spatialize = spatialize;
            Position = position;
            PositionSource = positionSource;
            RolloffMode = rolloffMode;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            MixerGroup = mixerGroup;
        }

        public static SoundOptions Default => new (
            volume: 1.0f,
            pitch: 1.0f,
            spatialize: false,
            position: default,
            positionSource: null,
            rolloffMode: AudioRolloffMode.Logarithmic,
            minDistance: 1.0f,
            maxDistance: 500.0f,
            mixerGroup: null
        );

        public static implicit operator SerializableSoundOptions(SoundOptions options)
        {
            return new SerializableSoundOptions()
            {
                BaseVolume = options.BaseVolume,
                BasePitch = options.BasePitch,
                Spatialize = options.Spatialize,
                Position = options.Position,
                PositionSource = options.PositionSource,
                RolloffMode = options.RolloffMode,
                MinDistance = options.MinDistance,
                MaxDistance = options.MaxDistance,
                MixerGroup = options.MixerGroup
            };
        }
    }

    [System.Serializable]
    public struct SerializableSoundOptions : ISoundOptions
    {
        [SerializeField] private float baseVolume;
        [SerializeField] private float basePitch;
        [SerializeField] private bool spatialize;
        [SerializeField] private Vector3 position;
        [SerializeField] private Transform positionSource;
        [SerializeField] private AudioRolloffMode rolloffMode;
        [SerializeField] private float minDistance;
        [SerializeField] private float maxDistance;
        [SerializeField] private AudioMixerGroup mixerGroup;

        public float BaseVolume { readonly get => baseVolume; set => baseVolume = value; }
        public float BasePitch { readonly get => basePitch; set => basePitch = value; }
        public bool Spatialize { readonly get => spatialize; set => spatialize = value; }
        public Vector3 Position { readonly get => position; set => position = value; }
        public Transform PositionSource { readonly get => positionSource; set => positionSource = value; }
        public AudioRolloffMode RolloffMode { readonly get => rolloffMode; set => rolloffMode = value; }
        public float MinDistance { readonly get => minDistance; set => minDistance = value; }
        public float MaxDistance { readonly get => maxDistance; set => maxDistance = value; }
        public AudioMixerGroup MixerGroup { readonly get => mixerGroup; set => mixerGroup = value; }

        public static implicit operator SoundOptions(SerializableSoundOptions options)
        {
            return new SoundOptions(
                volume: options.BaseVolume,
                pitch: options.BasePitch,
                spatialize: options.Spatialize,
                position: options.Position,
                positionSource: options.PositionSource,
                rolloffMode: options.RolloffMode,
                minDistance: options.MinDistance,
                maxDistance: options.MaxDistance,
                mixerGroup: options.MixerGroup
            );
        }
    }

    public static class SoundOptionsExtensions
    {
        public static SoundOptions WithVolume<T>(this T options, float volume) where T : ISoundOptions
        {
            return new SoundOptions(
                volume: volume,
                pitch: options.BasePitch,
                spatialize: options.Spatialize,
                position: options.Position,
                positionSource: options.PositionSource,
                rolloffMode: options.RolloffMode,
                minDistance: options.MinDistance,
                maxDistance: options.MaxDistance,
                mixerGroup: options.MixerGroup
            );
        }

        public static SoundOptions WithPitch<T>(this T options, float pitch) where T : ISoundOptions
        {
            return new SoundOptions(
                volume: options.BaseVolume,
                pitch: pitch,
                spatialize: options.Spatialize,
                position: options.Position,
                positionSource: options.PositionSource,
                rolloffMode: options.RolloffMode,
                minDistance: options.MinDistance,
                maxDistance: options.MaxDistance,
                mixerGroup: options.MixerGroup
            );
        }

        public static SoundOptions WithVolumeAndPitch<T>(this T options, float volume, float pitch) where T : ISoundOptions
        {
            return new SoundOptions(
                volume: volume,
                pitch: pitch,
                spatialize: options.Spatialize,
                position: options.Position,
                positionSource: options.PositionSource,
                rolloffMode: options.RolloffMode,
                minDistance: options.MinDistance,
                maxDistance: options.MaxDistance,
                mixerGroup: options.MixerGroup
            );
        }

        public static SoundOptions WithSpatialization<T>(this T options, bool spatialize) where T : ISoundOptions
        {
            return new SoundOptions(
                volume: options.BaseVolume,
                pitch: options.BasePitch,
                spatialize: spatialize,
                position: options.Position,
                positionSource: options.PositionSource,
                rolloffMode: options.RolloffMode,
                minDistance: options.MinDistance,
                maxDistance: options.MaxDistance,
                mixerGroup: options.MixerGroup
            );
        }

        public static SoundOptions WithPosition<T>(this T options, Vector3 position) where T : ISoundOptions
        {
            return new SoundOptions(
                volume: options.BaseVolume,
                pitch: options.BasePitch,
                spatialize: options.Spatialize,
                position: position,
                positionSource: options.PositionSource,
                rolloffMode: options.RolloffMode,
                minDistance: options.MinDistance,
                maxDistance: options.MaxDistance,
                mixerGroup: options.MixerGroup
            );
        }

        public static SoundOptions WithPositionSource<T>(this T options, Transform positionSource) where T : ISoundOptions
        {
            return new SoundOptions(
                volume: options.BaseVolume,
                pitch: options.BasePitch,
                spatialize: options.Spatialize,
                position: options.Position,
                positionSource: positionSource,
                rolloffMode: options.RolloffMode,
                minDistance: options.MinDistance,
                maxDistance: options.MaxDistance,
                mixerGroup: options.MixerGroup
            );
        }

        public static SoundOptions WithRolloffMode<T>(this T options, AudioRolloffMode rolloffMode) where T : ISoundOptions
        {
            return new SoundOptions(
                volume: options.BaseVolume,
                pitch: options.BasePitch,
                spatialize: options.Spatialize,
                position: options.Position,
                positionSource: options.PositionSource,
                rolloffMode: rolloffMode,
                minDistance: options.MinDistance,
                maxDistance: options.MaxDistance,
                mixerGroup: options.MixerGroup
            );
        }

        public static SoundOptions WithMinDistance<T>(this T options, float minDistance) where T : ISoundOptions
        {
            return new SoundOptions(
                volume: options.BaseVolume,
                pitch: options.BasePitch,
                spatialize: options.Spatialize,
                position: options.Position,
                positionSource: options.PositionSource,
                rolloffMode: options.RolloffMode,
                minDistance: minDistance,
                maxDistance: options.MaxDistance,
                mixerGroup: options.MixerGroup
            );
        }

        public static SoundOptions WithMaxDistance<T>(this T options, float maxDistance) where T : ISoundOptions
        {
            return new SoundOptions(
                volume: options.BaseVolume,
                pitch: options.BasePitch,
                spatialize: options.Spatialize,
                position: options.Position,
                positionSource: options.PositionSource,
                rolloffMode: options.RolloffMode,
                minDistance: options.MinDistance,
                maxDistance: maxDistance,
                mixerGroup: options.MixerGroup
            );
        }

        public static SoundOptions WithMixerGroup<T>(this T options, AudioMixerGroup mixerGroup) where T : ISoundOptions
        {
            return new SoundOptions(
                volume: options.BaseVolume,
                pitch: options.BasePitch,
                spatialize: options.Spatialize,
                position: options.Position,
                positionSource: options.PositionSource,
                rolloffMode: options.RolloffMode,
                minDistance: options.MinDistance,
                maxDistance: options.MaxDistance,
                mixerGroup: mixerGroup
            );
        }
    }
}