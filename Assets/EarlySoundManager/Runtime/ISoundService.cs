using UnityEngine;

namespace Early.SoundManager
{
    public interface ISoundService : System.IDisposable
    {
        float MasterVolume { get; }
        float BgmVolume { get; }
        float SeVolume { get; }
        event System.Action OnMasterVolumeChanged;
        event System.Action OnSeVolumeChanged;
        event System.Action OnBgmVolumeChanged;
        ISeHandle PlaySe(AudioClip clip);
        ISeHandle PlaySe(AudioClip clip, SoundOptions options);
        ISeHandle PlaySe(string key);
        ISeHandle PlaySe(string key, SoundOptions options);
        IBgmHandle PlayBgm(AudioClip clip, BgmTrackId trackId = default);
        IBgmHandle PlayBgm(AudioClip clip, SoundOptions options, BgmTrackId trackId = default);
        IBgmHandle PlayBgm(string key, BgmTrackId trackId = default);
        IBgmHandle PlayBgm(string key, SoundOptions options, BgmTrackId trackId = default);
        IBgmHandle SwitchBgm(AudioClip clip, BgmTrackId trackId = default);
        IBgmHandle SwitchBgm(AudioClip clip, SoundOptions options, BgmTrackId trackId = default);
        IBgmHandle SwitchBgm(AudioClip clip, SoundFadingOptions fadingOptions, BgmTrackId trackId = default);
        IBgmHandle SwitchBgm(AudioClip clip, SoundOptions options, SoundFadingOptions fadingOptions, BgmTrackId trackId = default);
        IBgmHandle SwitchBgm(string key, BgmTrackId trackId = default);
        IBgmHandle SwitchBgm(string key, SoundOptions options, BgmTrackId trackId = default);
        IBgmHandle SwitchBgm(string key, SoundFadingOptions fadingOptions, BgmTrackId trackId = default);
        IBgmHandle SwitchBgm(string key, SoundOptions options, SoundFadingOptions fadingOptions, BgmTrackId trackId = default);
        void SetMasterVolume(float volume);
        void SetSeVolume(float volume);
        void SetBgmVolume(float volume);
        void StopBgm(BgmTrackId trackId = default);
        void StopBgm(SoundFadingOptions fadingOptions, BgmTrackId trackId = default);
        internal void SetFadingTimer(ISoundHandle handle, SoundFadingStatus fadingStatus);
        internal void SetLink(ISoundHandle handle, GameObject target);
        internal void ForceCompleteFading(ISoundHandle handle);
        internal bool IsFading(ISoundHandle handle);
    }

    public interface ISoundHandle : System.IDisposable
    {
        float Volume { get; }
        float Pitch { get; }
        float BaseVolume { get; }
        float BasePitch { get; }
        float Time { get; }
        bool IsPlaying { get; }
        bool IsPaused { get; }
        bool IsValid { get; }
        event System.Action OnPaused;
        event System.Action OnResumed;
        event System.Action OnVolumeChanged;
        event System.Action OnPitchChanged;
        void Pause();
        void Pause(SoundFadingOptions fadingOptions);
        void Stop();
        void Stop(SoundFadingOptions fadingOptions);
        void Resume();
        void Resume(SoundFadingOptions fadingOptions);
        void SetVolume(float volume);
        void SetVolume(float volume, SoundFadingOptions fadingOptions);
        void SetPitch(float pitch);
        void SetPitch(float pitch, SoundFadingOptions fadingOptions);
        ISoundHandle SetLink(GameObject target);
        internal AudioSource Release();
    }

    public interface ISeHandle : ISoundHandle
    {
        event System.Action OnCompleted;
    }

    public interface IBgmHandle : ISoundHandle
    {
    }

    public interface ISoundPositionUpdatable
    {
        void UpdatePosition(Vector3 position);
    }

    internal interface IFadeCompletionNotifiable
    {
        event System.Action OnFadeCompleted;
        void NotifyFadeCompleted();
        void ForceCompleteFading();
        bool IsFading { get; }
    }
}