namespace Early.SoundManager
{
    public readonly struct SoundFadingOptions
    {
        public readonly float FadeDuration;
        public readonly CancelBehaviour CancelBehaviour;
        public readonly bool UseScaledTime;

        public SoundFadingOptions(float fadeDuration, CancelBehaviour cancelBehaviour = CancelBehaviour.Cancel, bool useScaledTime = false)
        {
            FadeDuration = fadeDuration;
            CancelBehaviour = cancelBehaviour;
            UseScaledTime = useScaledTime;
        }
    }

    [System.Serializable]
    public struct SerializableSoundFadingOptions
    {
        public float FadeDuration;
        public bool UseScaledTime;

        public static implicit operator SoundFadingOptions(SerializableSoundFadingOptions options)
        {
            return new SoundFadingOptions(options.FadeDuration, useScaledTime: options.UseScaledTime);
        }
    }

    public enum CancelBehaviour
    {
        Cancel,
        Complete,
        Ignore
    }
}