using UnityEngine;

namespace Early.SoundManager
{
    internal sealed class BgmHandle : IBgmHandle, ISoundPositionUpdatable, IFadeCompletionNotifiable
    {
        private readonly AudioSource audioSource;
        private readonly ISoundService soundService;
        private float previousBaseVolume = 1f;
        private bool isValid;

        public BgmHandle()
        {
        }
        
        public BgmHandle(AudioSource audioSource, ISoundService soundService)
        {
            this.audioSource = audioSource;
            this.soundService = soundService;
            soundService.OnMasterVolumeChanged += ApplyVolume;
            soundService.OnBgmVolumeChanged += ApplyVolume;
            isValid = true;
        }

#region IBgmHandle Implementation
        public float Volume => IsValid ? audioSource.volume : 0f;
        public float Pitch => IsValid ? audioSource.pitch : 0f;
        public float BaseVolume { get; private set; } = 1f;
        public float BasePitch { get; private set; } = 1f;
        public float Time => IsValid ? audioSource.time : 0f;
        public bool IsPlaying => IsValid && audioSource.isPlaying;
        public bool IsPaused { get; private set; } = false;
        // Unity operator on purpose: the AudioSource can be destroyed externally.
        public bool IsValid => isValid && audioSource != null;
        public event System.Action OnPaused;
        public event System.Action OnResumed;
        public event System.Action OnVolumeChanged;
        public event System.Action OnPitchChanged;
        internal event System.Action OnStopped;

        public void Stop()
        {
            // Guard on the field, not IsValid: a destroyed source must still raise OnStopped.
            if (!isValid) return;

            if (audioSource != null) audioSource.Stop();
            IsPaused = false;
            OnStopped?.Invoke();
        }

        public void Stop(SoundFadingOptions fadingOptions)
        {
            if (!isValid) return;
            if (audioSource == null)
            {
                Stop();
                return;
            }

            soundService.SetFadingTimer(this, new SoundFadingStatus(
                SoundFadingType.Volume,
                fadingOptions.FadeDuration,
                BaseVolume,
                0,
                fadingOptions.UseScaledTime,
                () => Stop()
            ));
        }

        public void Pause()
        {
            if (!IsValid || IsPaused) return;

            audioSource.Pause();
            IsPaused = true;
            OnPaused?.Invoke();
        }

        public void Pause(SoundFadingOptions fadingOptions)
        {
            if (!IsValid || IsPaused) return;

            previousBaseVolume = BaseVolume;
            soundService.SetFadingTimer(this, new SoundFadingStatus(
                SoundFadingType.Volume,
                fadingOptions.FadeDuration,
                BaseVolume,
                0,
                fadingOptions.UseScaledTime,
                () => Pause()
            ));
        }

        public void Resume()
        {
            if (!IsValid || !IsPaused) return;

            audioSource.UnPause();
            IsPaused = false;
            OnResumed?.Invoke();
        }

        public void Resume(SoundFadingOptions fadingOptions)
        {
            if (!IsValid || !IsPaused) return;

            SetVolume(0);
            Resume();
            soundService.SetFadingTimer(this, new SoundFadingStatus(
                SoundFadingType.Volume,
                fadingOptions.FadeDuration,
                0,
                previousBaseVolume,
                fadingOptions.UseScaledTime
            ));
        }

        public void SetVolume(float volume)
        {
            if (!IsValid) return;

            BaseVolume = volume;
            ApplyVolume();
        }

        public void SetVolume(float volume, SoundFadingOptions fadingOptions)
        {
            if (!IsValid) return;

            soundService.SetFadingTimer(this, new SoundFadingStatus(
                SoundFadingType.Volume,
                fadingOptions.FadeDuration,
                BaseVolume,
                volume,
                fadingOptions.UseScaledTime
            ));
        }

        public void SetPitch(float pitch)
        {
            if (!IsValid) return;

            BasePitch = pitch;
            ApplyPitch();
        }

        public void SetPitch(float pitch, SoundFadingOptions fadingOptions)
        {
            if (!IsValid) return;

            soundService.SetFadingTimer(this, new SoundFadingStatus(
                SoundFadingType.Pitch,
                fadingOptions.FadeDuration,
                BasePitch,
                pitch,
                fadingOptions.UseScaledTime
            ));
        }

        public ISoundHandle SetLink(GameObject target)
        {
            if (IsValid) soundService.SetLink(this, target);
            return this;
        }

        AudioSource ISoundHandle.Release()
        {
            Dispose();
            return audioSource;
        }

        public void Dispose()
        {
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.clip = null;
            }
            isValid = false;
            if (soundService == null) return;

            soundService.OnMasterVolumeChanged -= ApplyVolume;
            soundService.OnBgmVolumeChanged -= ApplyVolume;
        }
#endregion

#region ISoundPositionUpdatable Implementation
        public void UpdatePosition(Vector3 position)
        {
            if (audioSource != null)
            {
                audioSource.transform.position = position;
            }
        }
#endregion

#region IFadeCompletionNotifiable Implementation
        private event System.Action fadeCompleted;
        event System.Action IFadeCompletionNotifiable.OnFadeCompleted
        {
            add => fadeCompleted += value;
            remove => fadeCompleted -= value;
        }
        void IFadeCompletionNotifiable.NotifyFadeCompleted() => fadeCompleted?.Invoke();
        void IFadeCompletionNotifiable.ForceCompleteFading() => soundService.ForceCompleteFading(this);
        bool IFadeCompletionNotifiable.IsFading => soundService.IsFading(this);
#endregion

#region Private Helper Methods
        private void ApplyVolume()
        {
            if (!IsValid) return;

            audioSource.volume = BaseVolume * soundService.BgmVolume * soundService.MasterVolume;
            OnVolumeChanged?.Invoke();
        }

        private void ApplyPitch()
        {
            if (!IsValid) return;

            audioSource.pitch = BasePitch;
            OnPitchChanged?.Invoke();
        }
#endregion
    }
}    