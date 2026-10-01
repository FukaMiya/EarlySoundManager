using System;
using System.Threading;

#if UNITASK_SUPPORT
using AsyncResult = Cysharp.Threading.Tasks.UniTask;
using AsyncSource = Cysharp.Threading.Tasks.UniTaskCompletionSource;
#elif UNITY_2023_1_OR_NEWER
using UnityEngine;
using AsyncResult = UnityEngine.Awaitable;
using AsyncSource = UnityEngine.AwaitableCompletionSource;
#else
using System.Threading.Tasks;
using AsyncResult = System.Threading.Tasks.ValueTask;
#endif

namespace Early.SoundManager
{
    public static class SoundHandleFadeAsyncExtensions
    {
        #region Public API

        public static AsyncResult StopAsync(this ISoundHandle handle, SoundFadingOptions fadingOptions, CancellationToken cancellationToken = default)
            => HandleAsyncOperation(handle, fadingOptions, cancellationToken, h => h.IsPlaying, h => h.Stop(), h => h.Stop(fadingOptions));

        public static AsyncResult PauseAsync(this ISoundHandle handle, SoundFadingOptions fadingOptions, CancellationToken cancellationToken = default)
            => HandleAsyncOperation(handle, fadingOptions, cancellationToken, h => !h.IsPaused, h => h.Pause(), h => h.Pause(fadingOptions));

        public static AsyncResult ResumeAsync(this ISoundHandle handle, SoundFadingOptions fadingOptions, CancellationToken cancellationToken = default)
            => HandleAsyncOperation(handle, fadingOptions, cancellationToken, h => h.IsPaused, h => h.Resume(), h => h.Resume(fadingOptions));

        public static AsyncResult SetVolumeAsync(this ISoundHandle handle, float volume, SoundFadingOptions fadingOptions, CancellationToken cancellationToken = default)
            => HandleAsyncOperation(handle, fadingOptions, cancellationToken, h => true, h => h.SetVolume(volume), h => h.SetVolume(volume, fadingOptions));

        public static AsyncResult SetPitchAsync(this ISoundHandle handle, float pitch, SoundFadingOptions fadingOptions, CancellationToken cancellationToken = default)
            => HandleAsyncOperation(handle, fadingOptions, cancellationToken, h => true, h => h.SetPitch(pitch), h => h.SetPitch(pitch, fadingOptions));

        public static AsyncResult WaitForCurrentFadeAsync(this ISoundHandle handle, CancelBehaviour cancelBehaviour = CancelBehaviour.Cancel, CancellationToken cancellationToken = default)
        {
            if (handle is not IFadeCompletionNotifiable notifiable || !notifiable.IsFading)
                return GetCompleted();
            return FadeAsync(notifiable, cancelBehaviour, cancellationToken);
        }

        #endregion

        #region Core Logic

        private static AsyncResult HandleAsyncOperation<T>(
            T handle,
            SoundFadingOptions fadingOptions,
            CancellationToken cancellationToken,
            Func<T, bool> precondition,
            Action<T> instantAction,
            Action<T> fadeAction) where T : ISoundHandle
        {
            if (!handle.IsValid || !precondition(handle)) return GetCompleted();

            if (fadingOptions.FadeDuration <= 0f)
            {
                instantAction(handle);
                return GetCompleted();
            }

            fadeAction(handle);
            return handle is IFadeCompletionNotifiable notifiable
                ? FadeAsync(notifiable, fadingOptions.CancelBehaviour, cancellationToken)
                : GetCompleted();
        }

        private static AsyncResult FadeAsync(IFadeCompletionNotifiable notifiable, CancelBehaviour cancelBehaviour, CancellationToken cancellationToken)
        {
#if !UNITASK_SUPPORT && !UNITY_2023_1_OR_NEWER
            var tcs = new TaskCompletionSource<bool>();
#else
            var source = new AsyncSource();
#endif

            void OnFadeCompleted()
            {
                notifiable.OnFadeCompleted -= OnFadeCompleted;
#if UNITASK_SUPPORT
                source.TrySetResult();
#elif UNITY_2023_1_OR_NEWER
                source.SetResult();
#else
                tcs.TrySetResult(true);
#endif
            }

            notifiable.OnFadeCompleted += OnFadeCompleted;

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    switch (cancelBehaviour)
                    {
                        case CancelBehaviour.Cancel:
                            notifiable.OnFadeCompleted -= OnFadeCompleted;
#if UNITASK_SUPPORT
                            source.TrySetCanceled();
#elif UNITY_2023_1_OR_NEWER
                            source.SetCanceled();
#else
                            tcs.TrySetCanceled();
#endif
                            break;
                        case CancelBehaviour.Complete:
                            notifiable.OnFadeCompleted -= OnFadeCompleted;
                            notifiable.ForceCompleteFading();
#if UNITASK_SUPPORT
                            source.TrySetResult();
#elif UNITY_2023_1_OR_NEWER
                            source.SetResult();
#else
                            tcs.TrySetResult(true);
#endif
                            break;
                    }
                });
            }

#if UNITASK_SUPPORT
            return source.Task;
#elif UNITY_2023_1_OR_NEWER
            return source.Awaitable;
#else
            return new ValueTask(tcs.Task);
#endif
        }

        private static AsyncResult GetCompleted()
        {
#if UNITASK_SUPPORT
            return AsyncResult.CompletedTask;
#elif UNITY_2023_1_OR_NEWER
            var source = new AsyncSource();
            source.SetResult();
            return source.Awaitable;
#else
            return default;
#endif
        }

        #endregion
    }
}
