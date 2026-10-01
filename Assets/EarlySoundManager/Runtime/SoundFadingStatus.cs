namespace Early.SoundManager
{
    internal enum SoundFadingType
    {
        Volume,
        Pitch
    }

    internal sealed class SoundFadingStatus
    {
        public SoundFadingType FadingType { get; set; }
        public float Timer { get; set; }
        public float Duration { get; set; }
        public float StartValue { get; set; }
        public float EndValue { get; set; }
        public bool UseScaledTime { get; set; }
        public System.Action OnCompleted { get; set; }

        public SoundFadingStatus(SoundFadingType fadingType, float duration, float startValue, float endValue, bool useScaledTime, System.Action onCompleted = null)
        {
            FadingType = fadingType;
            Timer = 0f;
            Duration = duration;
            StartValue = startValue;
            EndValue = endValue;
            UseScaledTime = useScaledTime;
            OnCompleted = onCompleted;
        }
    }
}