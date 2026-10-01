using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

namespace Early.SoundManager
{
    public sealed class SoundManager : ISoundService, ITickable
    {
        private const int defaultPoolCapacity = 10;
        private const int defaultPoolMaxSize = 20;

        private readonly ObjectPool<AudioSource> availableAudioSources;
        private readonly List<ISeHandle> activeSeHandles = new ();
        private readonly Dictionary<string, AudioClip> audioClipCache = new ();
        private readonly Dictionary<ISoundHandle, SoundFadingStatus> fadingTimers = new ();
        private readonly Dictionary<ISoundHandle, Transform> soundPositionSources = new ();
        private readonly Dictionary<ISoundHandle, GameObject> links = new ();
        private readonly List<ISoundHandle> handlesToRemove = new ();
        private readonly Dictionary<BgmTrackId, BgmTrackState> bgmTracks = new ();

        public AudioMixerGroup DefaultSeMixerGroup { get; set; }
        public AudioMixerGroup DefaultBgmMixerGroup { get; set; }

        public SoundManager() : this(null) { }
        public SoundManager(SoundRegistry SoundRegistry)
        {
            this.availableAudioSources = new ObjectPool<AudioSource>
            (
                createFunc: OnCreatePoolObject,
                actionOnGet: OnGetFromPool,
                actionOnRelease: OnReleaseToPool,
                actionOnDestroy: OnDestroyPoolObject,
                collectionCheck: true,
                defaultCapacity: defaultPoolCapacity,
                maxSize: defaultPoolMaxSize
            );

            SetupSoundRegistry(SoundRegistry);
        }

        public void Tick()
        {
            CheckLinks();
            CheckSeCompletion();
            FadeHandles();
            MoveSoundPositions();
        }

#region ISoundService Implementation
        public float MasterVolume { get; private set; } = 1.0f;
        public float SeVolume { get; private set; } = 1.0f;
        public float BgmVolume { get; private set; } = 1.0f;

        public event System.Action OnMasterVolumeChanged;
        public event System.Action OnSeVolumeChanged;
        public event System.Action OnBgmVolumeChanged;

        public ISeHandle PlaySe(AudioClip clip)
        {
            return PlaySe(clip, SoundOptions.Default);
        }

        public ISeHandle PlaySe(AudioClip clip, SoundOptions options)
        {
            var handle = PlaySeInternal(GetAvailableAudioSource(), clip, options);
            handle.OnCompleted += () =>
            {
                ReleaseToPool(handle.Release());
                activeSeHandles.Remove(handle);
            };
            activeSeHandles.Add(handle);
            return handle;
        }

        public ISeHandle PlaySe(string key)
        {
            return PlaySe(key, SoundOptions.Default);
        }

        public ISeHandle PlaySe(string key, SoundOptions options)
        {
            if (TryGetAudioClipByKey(key, out var clip))
            {
                return PlaySe(clip, options);
            }
            else
            {
                return new SeHandle();
            }
        }

        public IBgmHandle PlayBgm(AudioClip clip, BgmTrackId trackId = default)
        {
            return PlayBgm(clip, SoundOptions.Default, trackId);
        }

        public IBgmHandle PlayBgm(AudioClip clip, SoundOptions options, BgmTrackId trackId = default)
        {
            trackId = ResolveTrackId(trackId);
            if (bgmTracks.TryGetValue(trackId, out var trackState) && trackState.Current != null && trackState.Current.IsValid)
            {
                return SwitchBgm(clip, options, trackId);
            }

            trackState = new BgmTrackState(PlayBgmInternal(GetAvailableAudioSource(), clip, options, trackId));
            bgmTracks[trackId] = trackState;
            return trackState.Current;
        }

        public IBgmHandle PlayBgm(string key, BgmTrackId trackId = default)
        {
            return PlayBgm(key, SoundOptions.Default, trackId);
        }

        public IBgmHandle PlayBgm(string key, SoundOptions options, BgmTrackId trackId = default)
        {
            if (TryGetAudioClipByKey(key, out var clip))
            {
                return PlayBgm(clip, options, trackId);
            }
            else
            {
                return new BgmHandle();
            }
        }

        public IBgmHandle SwitchBgm(AudioClip clip, BgmTrackId trackId = default)
        {
            return SwitchBgm(clip, SoundOptions.Default, trackId);
        }

        public IBgmHandle SwitchBgm(AudioClip clip, SoundOptions options, BgmTrackId trackId = default)
        {
            trackId = ResolveTrackId(trackId);
            if (bgmTracks.TryGetValue(trackId, out var trackState) && trackState.Current != null && trackState.Current.IsValid)
            {
                StopBgm(trackId);
            }

            return PlayBgm(clip, options, trackId);
        }

        public IBgmHandle SwitchBgm(AudioClip clip, SoundFadingOptions fadingOptions, BgmTrackId trackId = default)
        {
            return SwitchBgm(clip, SoundOptions.Default, fadingOptions, trackId);
        }

        public IBgmHandle SwitchBgm(AudioClip clip, SoundOptions options, SoundFadingOptions fadingOptions, BgmTrackId trackId = default)
        {
            trackId = ResolveTrackId(trackId);
            if (!bgmTracks.TryGetValue(trackId, out var trackState) || trackState.Current == null || !trackState.Current.IsValid || fadingOptions.FadeDuration <= 0f)
            {
                return PlayBgm(clip, options, trackId);
            }

            if (trackState.Next != null)
            {
                fadingTimers.Remove(trackState.Current);
                fadingTimers.Remove(trackState.Next);
                ReleaseToPool(trackState.Current.Release());
                trackState.Current = trackState.Next;
                trackState.Next = null;
            }

            return SwitchBgmInternal(GetAvailableAudioSource(), clip, options, fadingOptions, trackState, trackId);
        }

        public IBgmHandle SwitchBgm(string key, BgmTrackId trackId = default)
        {
            return SwitchBgm(key, SoundOptions.Default, trackId);
        }

        public IBgmHandle SwitchBgm(string key, SoundOptions options, BgmTrackId trackId = default)
        {
            if (TryGetAudioClipByKey(key, out var clip))
            {
                return SwitchBgm(clip, options, trackId);
            }
            else
            {
                return new BgmHandle();
            }
        }

        public IBgmHandle SwitchBgm(string key, SoundFadingOptions fadingOptions, BgmTrackId trackId = default)
        {
            return SwitchBgm(key, SoundOptions.Default, fadingOptions, trackId);
        }

        public IBgmHandle SwitchBgm(string key, SoundOptions options, SoundFadingOptions fadingOptions, BgmTrackId trackId = default)
        {
            if (TryGetAudioClipByKey(key, out var clip))
            {
                return SwitchBgm(clip, options, fadingOptions, trackId);
            }
            else
            {
                return new BgmHandle();
            }
        }

        public void StopBgm(BgmTrackId trackId = default)
        {
            if (!bgmTracks.TryGetValue(ResolveTrackId(trackId), out var trackState)) return;

            var current = trackState.Current;
            var next = trackState.Next;
            current?.Stop();
            next?.Stop();
        }

        public void StopBgm(SoundFadingOptions fadingOptions, BgmTrackId trackId = default)
        {
            if (!bgmTracks.TryGetValue(ResolveTrackId(trackId), out var trackState)) return;

            trackState.Current?.Stop(fadingOptions);
            trackState.Next?.Stop(fadingOptions);
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = volume;
            OnMasterVolumeChanged?.Invoke();
        }

        public void SetSeVolume(float volume)
        {
            SeVolume = volume;
            OnSeVolumeChanged?.Invoke();
        }

        public void SetBgmVolume(float volume)
        {
            BgmVolume = volume;
            OnBgmVolumeChanged?.Invoke();
        }

        void ISoundService.SetFadingTimer(ISoundHandle handle, SoundFadingStatus fadingStatus)
        {
            if (handle != null && handle.IsValid)
            {
                fadingTimers[handle] = fadingStatus;
            }
        }

        void ISoundService.SetLink(ISoundHandle handle, GameObject target)
        {
            if (handle == null || !handle.IsValid) return;

            // Unity operator on purpose: a destroyed target also unlinks.
            if (target == null)
            {
                links.Remove(handle);
            }
            else
            {
                links[handle] = target;
            }
        }

        bool ISoundService.IsFading(ISoundHandle handle) => fadingTimers.ContainsKey(handle);

        void ISoundService.ForceCompleteFading(ISoundHandle handle)
        {
            if (!fadingTimers.TryGetValue(handle, out var status)) return;

            fadingTimers.Remove(handle);

            if (status.FadingType == SoundFadingType.Volume)
                handle.SetVolume(status.EndValue);
            else
                handle.SetPitch(status.EndValue);

            status.OnCompleted?.Invoke();
            (handle as IFadeCompletionNotifiable)?.NotifyFadeCompleted();
        }

        public void Dispose()
        {
            foreach (var trackState in bgmTracks.Values)
            {
                if (trackState.Current != null && trackState.Current.IsValid)
                {
                    ReleaseToPool(trackState.Current.Release());
                }
                if (trackState.Next != null && trackState.Next.IsValid)
                {
                    ReleaseToPool(trackState.Next.Release());
                }
            }
            audioClipCache.Clear();
            fadingTimers.Clear();
            soundPositionSources.Clear();
            links.Clear();
            foreach (var handle in activeSeHandles)
            {
                ReleaseToPool(handle.Release());
            }
            activeSeHandles.Clear();
            availableAudioSources.Clear();
        }
#endregion

#region Private Helper Methods
        private bool TryGetAudioClipByKey(string key, out AudioClip clip)
        {
            if (audioClipCache.TryGetValue(key, out clip))
            {
                return true;
            }
            else
            {
                Debug.LogWarning($"Sound with key '{key}' not found in registry.");
                clip = null;
                return false;
            }
        }

        private AudioSource GetAvailableAudioSource()
        {
            return availableAudioSources.Get();
        }

        private AudioSource SetAudioSourceParams(ISoundHandle handle, AudioSource audioSource, SoundOptions options, AudioMixerGroup defaultMixerGroup)
        {
            handle.SetVolume(options.BaseVolume);
            handle.SetPitch(options.BasePitch);
            audioSource.spatialBlend = options.Spatialize ? 1.0f : 0.0f;
            audioSource.rolloffMode = options.RolloffMode;
            audioSource.minDistance = options.MinDistance;
            audioSource.maxDistance = options.MaxDistance;
            audioSource.transform.position = options.Position;
            audioSource.outputAudioMixerGroup = options.MixerGroup != null ? options.MixerGroup : defaultMixerGroup;
            if (options.PositionSource != null)
            {
                soundPositionSources[handle] = options.PositionSource;
            }
            return audioSource;
        }

        private BgmTrackId ResolveTrackId(BgmTrackId trackId)
        {
            return trackId.Equals(default) ? BgmTrackId.Main : trackId;
        }
    
        private ISeHandle PlaySeInternal(AudioSource audioSource, AudioClip clip, SoundOptions options)
        {
            var handle = new SeHandle(audioSource, this);
            audioSource.loop = false;
            audioSource.clip = clip;
            SetAudioSourceParams(handle, audioSource, options, DefaultSeMixerGroup);
            audioSource.Play();
            return handle;
        }

        private IBgmHandle PlayBgmInternal(AudioSource audioSource, AudioClip clip, SoundOptions options, BgmTrackId trackId)
        {
            var handle = new BgmHandle(audioSource, this);
            handle.OnStopped += () => OnBgmStopped(handle, trackId);
            audioSource.loop = true;
            audioSource.clip = clip;
            SetAudioSourceParams(handle, audioSource, options, DefaultBgmMixerGroup);
            audioSource.Play();
            return handle;
        }

        private IBgmHandle SwitchBgmInternal(AudioSource audioSource, AudioClip clip, SoundOptions options, SoundFadingOptions fadingOptions, BgmTrackState trackState, BgmTrackId trackId)
        {
            var next = new BgmHandle(audioSource, this);
            next.OnStopped += () => OnBgmStopped(next, trackId);
            trackState.Next = next;
            fadingTimers[trackState.Next] = new SoundFadingStatus(SoundFadingType.Volume, fadingOptions.FadeDuration, 0, options.BaseVolume, fadingOptions.UseScaledTime, null);
            fadingTimers[trackState.Current] = new SoundFadingStatus(
                SoundFadingType.Volume,
                fadingOptions.FadeDuration,
                trackState.Current.BaseVolume,
                0,
                fadingOptions.UseScaledTime,
                () =>
                {
                    if (trackState.Current != null && trackState.Current.IsValid)
                    {
                        ReleaseToPool(trackState.Current.Release());
                    }
                    trackState.Current = trackState.Next;
                    trackState.Next = null;
                    RemoveTrackIfEmpty(trackId, trackState);
                }
            );

            audioSource.loop = true;
            audioSource.clip = clip;
            SetAudioSourceParams(trackState.Next, audioSource, options, DefaultBgmMixerGroup);
            trackState.Next.SetVolume(0);
            audioSource.Play();
            return trackState.Next;
        }

        private void OnBgmStopped(BgmHandle handle, BgmTrackId trackId)
        {
            fadingTimers.Remove(handle);
            soundPositionSources.Remove(handle);
            links.Remove(handle);
            ReleaseToPool(((ISoundHandle)handle).Release());

            if (!bgmTracks.TryGetValue(trackId, out var trackState)) return;

            if (trackState.Next == handle)
            {
                trackState.Next = null;
            }
            else if (trackState.Current == handle)
            {
                trackState.Current = trackState.Next;
                trackState.Next = null;
            }
            RemoveTrackIfEmpty(trackId, trackState);
        }

        private void RemoveTrackIfEmpty(BgmTrackId trackId, BgmTrackState trackState)
        {
            if (trackState.Current == null && trackState.Next == null)
            {
                bgmTracks.Remove(trackId);
            }
        }

        private void CheckLinks()
        {
            handlesToRemove.Clear();
            foreach (var (handle, target) in links)
            {
                if (handle == null || !handle.IsValid || target == null)
                {
                    handlesToRemove.Add(handle);
                }
            }

            foreach (var handle in handlesToRemove)
            {
                links.Remove(handle);
                handle?.Stop();
            }
        }

        private void CheckSeCompletion()
        {
            for (int i = activeSeHandles.Count - 1; i >= 0; i--)
            {
                var handle = activeSeHandles[i];
                if (fadingTimers.ContainsKey(handle) || handle.IsPaused) continue; 
                if (!handle.IsPlaying)
                {
                    handle.Stop();
                }
            }
        }        

        private void FadeHandles()
        {
            handlesToRemove.Clear();
            foreach (var (handle, fadingStatus) in fadingTimers)
            {
                if (fadingStatus.Duration <= 0) continue;

                var delta = fadingStatus.UseScaledTime ? Time.deltaTime : Time.unscaledDeltaTime;
                if (delta <= 0f) continue;

                fadingStatus.Timer += delta;
                var t = Mathf.InverseLerp(0, fadingStatus.Duration, fadingStatus.Timer);
                var newValue = Mathf.Lerp(fadingStatus.StartValue, fadingStatus.EndValue, t);
                if (fadingStatus.FadingType == SoundFadingType.Volume)
                {
                    handle.SetVolume(newValue);
                }
                else if (fadingStatus.FadingType == SoundFadingType.Pitch)
                {
                    handle.SetPitch(newValue);
                }

                if (fadingStatus.Timer >= fadingStatus.Duration)
                {
                    handlesToRemove.Add(handle);
                }
            }

            foreach (var handle in handlesToRemove)
            {
                var status = fadingTimers[handle];
                fadingTimers.Remove(handle);
                status.OnCompleted?.Invoke();
                (handle as IFadeCompletionNotifiable)?.NotifyFadeCompleted();
            }
        }

        private void MoveSoundPositions()
        {
            handlesToRemove.Clear();
            foreach (var (handle, sourceTransform) in soundPositionSources)
            {
                if (handle == null || !handle.IsValid || sourceTransform == null)
                {
                    handlesToRemove.Add(handle);
                    continue;
                }

                if (handle is ISoundPositionUpdatable updatable)
                {
                    updatable.UpdatePosition(sourceTransform.position);
                }
            }

            foreach (var handle in handlesToRemove)
            {
                soundPositionSources.Remove(handle);
            }
        }

        private void SetupSoundRegistry(SoundRegistry SoundRegistry)
        {
            if (SoundRegistry == null)
            {
                return;
            }

            DefaultSeMixerGroup = SoundRegistry.DefaultSeMixerGroup;
            DefaultBgmMixerGroup = SoundRegistry.DefaultBgmMixerGroup;
            if (SoundRegistry.SoundEntries.Length == 0)
            {
                Debug.LogWarning("Sound registry is empty.");
                return;
            }

            foreach (var entry in SoundRegistry.SoundEntries)
            {
                if (!audioClipCache.ContainsKey(entry.key))
                {
                    audioClipCache.Add(entry.key, entry.clip);
                }
                else
                {
                    Debug.LogWarning($"Duplicate key '{entry.key}' found in sound registry. Skipping.");
                }
            }
        }
#endregion

#region Object Pooling Callbacks
        private AudioSource OnCreatePoolObject()
        {
            var audioSource = new GameObject("PooledAudioSource").AddComponent<AudioSource>();
            Object.DontDestroyOnLoad(audioSource.gameObject);
            return audioSource;
        }

        private void OnGetFromPool(AudioSource audioSource)
        {
            audioSource.gameObject.SetActive(true);
        }

        private void ReleaseToPool(AudioSource audioSource)
        {
            if (audioSource == null) return;

            availableAudioSources.Release(audioSource);
        }

        private void OnReleaseToPool(AudioSource audioSource)
        {
            if (audioSource == null) return;

            audioSource.Stop();
            audioSource.clip = null;
            audioSource.outputAudioMixerGroup = null;
            audioSource.gameObject.SetActive(false);
        }

        private void OnDestroyPoolObject(AudioSource audioSource)
        {
            if (audioSource == null) return;

            Object.Destroy(audioSource.gameObject);
        }
#endregion
    }

    public interface ITickable
#if VCONTAINER_SUPPORT
    : VContainer.Unity.ITickable
    {
    }
#else
    {
        void Tick();
    }
#endif
}