using System.Collections;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Early.SoundManager.Tests
{
    public sealed class SoundManagerTests
    {
        private SoundManager soundManager;
        private AudioClip clip;

        [SetUp]
        public void SetUp()
        {
            soundManager = new SoundManager();
            clip = AudioClip.Create("test", 44100, 1, 44100, false);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            soundManager.Dispose();
            Object.DestroyImmediate(clip);
        }

        private static AudioSource[] FindPooledAudioSources() =>
            Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(a => a.name == "PooledAudioSource").ToArray();

        [Test]
        public void DummyHandle_Dispose_DoesNotThrow()
        {
            LogAssert.Expect(LogType.Warning, new Regex("not found"));
            LogAssert.Expect(LogType.Warning, new Regex("not found"));
            var se = soundManager.PlaySe("missing");
            var bgm = soundManager.PlayBgm("missing");

            Assert.DoesNotThrow(() => se.Dispose());
            Assert.DoesNotThrow(() => bgm.Dispose());
            Assert.DoesNotThrow(() => se.SetLink(null));
            Assert.IsFalse(se.IsValid);
            Assert.IsFalse(bgm.IsValid);
        }

        [UnityTest]
        public IEnumerator Dispose_LeavesNoPooledAudioSource()
        {
            soundManager.PlaySe(clip);
            soundManager.PlayBgm(clip);
            Assert.IsNotEmpty(FindPooledAudioSources());

            soundManager.Dispose();
            yield return null;

            Assert.IsEmpty(FindPooledAudioSources());
        }

        [Test]
        public void StaleHandle_IsNotPlaying_AfterSourceIsReused()
        {
            var stale = soundManager.PlaySe(clip);
            stale.Stop();
            soundManager.PlaySe(clip);

            Assert.IsFalse(stale.IsValid);
            Assert.IsFalse(stale.IsPlaying);
        }

        [UnityTest]
        public IEnumerator SetLink_DestroyTarget_InvalidatesHandleOnNextTick()
        {
            var target = new GameObject("LinkTarget");
            var handle = soundManager.PlaySe(clip).SetLink(target);
            soundManager.Tick();
            Assert.IsTrue(handle.IsValid);

            Object.Destroy(target);
            yield return null;
            soundManager.Tick();

            Assert.IsFalse(handle.IsValid);
        }

        [Test]
        public void StopBgm_DeactivatesPooledAudioSource()
        {
            var handle = soundManager.PlayBgm(clip);
            Assert.IsTrue(FindPooledAudioSources().Any(a => a.gameObject.activeSelf));

            soundManager.StopBgm();

            Assert.IsFalse(handle.IsValid);
            Assert.IsFalse(FindPooledAudioSources().Any(a => a.gameObject.activeSelf));
        }

        private static AudioSource[] FindActivePooledAudioSources() =>
            FindPooledAudioSources().Where(a => a.gameObject.activeSelf).ToArray();

        private IEnumerator TickForSeconds(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                soundManager.Tick();
            }
        }

        [UnityTest]
        public IEnumerator SwitchBgm_WithFade_CrossfadesToNewHandle()
        {
            var old = soundManager.PlayBgm(clip);
            var next = soundManager.SwitchBgm(clip, new SoundFadingOptions(0.5f));

            yield return TickForSeconds(0.7f);

            Assert.IsFalse(old.IsValid);
            Assert.IsTrue(next.IsValid);
            Assert.AreEqual(1f, next.Volume, 0.01f);
        }

        [UnityTest]
        public IEnumerator SwitchBgm_WithFade_WhileVolumeFading_DoesNotThrow()
        {
            var first = soundManager.PlayBgm(clip);
            first.SetVolume(0.5f, new SoundFadingOptions(5f));
            IBgmHandle next = null;

            Assert.DoesNotThrow(() => next = soundManager.SwitchBgm(clip, new SoundFadingOptions(0.2f)));
            yield return TickForSeconds(0.4f);

            Assert.IsTrue(next.IsValid);
            Assert.IsFalse(first.IsValid);
            Assert.AreEqual(1, FindActivePooledAudioSources().Length);
        }

        [UnityTest]
        public IEnumerator SwitchBgm_NoFade_DuringCrossfade_StopsBothPreviousHandles()
        {
            var a = soundManager.PlayBgm(clip);
            var b = soundManager.SwitchBgm(clip, new SoundFadingOptions(0.5f));
            yield return null;
            soundManager.Tick();

            var c = soundManager.SwitchBgm(clip);
            yield return TickForSeconds(0.7f);

            Assert.IsFalse(a.IsValid);
            Assert.IsFalse(b.IsValid);
            Assert.IsTrue(c.IsValid);
            Assert.AreEqual(1, FindActivePooledAudioSources().Length);
        }

        [UnityTest]
        public IEnumerator Fade_UsesUnscaledTimeByDefault_AndIgnoresItWhenScaled()
        {
            Time.timeScale = 0f;
            var unscaled = soundManager.PlayBgm(clip);
            unscaled.SetVolume(0f, new SoundFadingOptions(0.2f));
            var scaled = soundManager.PlayBgm(clip, new BgmTrackId("Other"));
            scaled.SetVolume(0f, new SoundFadingOptions(0.2f, useScaledTime: true));

            yield return TickForSeconds(0.3f);

            Assert.AreEqual(0f, unscaled.Volume, 0.01f);
            Assert.AreEqual(1f, scaled.Volume, 0.01f);
        }

        [UnityTest]
        public IEnumerator SetLink_Null_UnlinksSoThatDestroyDoesNotStop()
        {
            var target = new GameObject("LinkTarget");
            var handle = soundManager.PlaySe(clip).SetLink(target).SetLink(null);

            Object.Destroy(target);
            yield return null;
            soundManager.Tick();

            Assert.IsTrue(handle.IsValid);
        }

        [UnityTest]
        public IEnumerator FrozenScaledFade_AudioSourceDestroyed_CancelDoesNotThrow()
        {
            Time.timeScale = 0f;
            var listener = new GameObject("Listener", typeof(AudioListener));
            var handle = soundManager.PlayBgm(clip);
            using var cts = new CancellationTokenSource();
            _ = handle.SetVolumeAsync(0f, new SoundFadingOptions(1f, CancelBehaviour.Complete, true), cts.Token);
            yield return null;
            soundManager.Tick();

            foreach (var source in FindPooledAudioSources()) Object.Destroy(source.gameObject);
            yield return null;

            Assert.DoesNotThrow(() => cts.Cancel());
            Assert.IsFalse(handle.IsValid);
            Assert.DoesNotThrow(() => soundManager.Tick());
            Assert.DoesNotThrow(() => soundManager.Dispose());
            LogAssert.NoUnexpectedReceived();
            Object.Destroy(listener);
        }

        [UnityTest]
        public IEnumerator FrozenScaledFade_DoesNotWriteVolume()
        {
            Time.timeScale = 0f;
            var handle = soundManager.PlayBgm(clip);
            var changed = 0;
            handle.SetVolume(0f, new SoundFadingOptions(1f, useScaledTime: true));
            handle.OnVolumeChanged += () => changed++;

            for (var i = 0; i < 5; i++)
            {
                yield return null;
                soundManager.Tick();
            }

            Assert.AreEqual(0, changed);
            Assert.AreEqual(1f, handle.Volume, 0.001f);
        }

        [UnityTest]
        public IEnumerator PlaySe_AudioSourceDestroyed_CompletesOnce()
        {
            var listener = new GameObject("Listener", typeof(AudioListener));
            var handle = soundManager.PlaySe(clip);
            var completed = 0;
            handle.OnCompleted += () => completed++;
            var awaiter = soundManager.PlaySeAsync(clip).GetAwaiter();
            foreach (var source in FindPooledAudioSources()) Object.Destroy(source.gameObject);
            yield return null;

            Assert.DoesNotThrow(() => soundManager.Tick());
            Assert.DoesNotThrow(() => soundManager.Tick());

            Assert.AreEqual(1, completed);
            Assert.IsTrue(awaiter.IsCompleted);
            Assert.IsFalse(handle.IsValid);
            LogAssert.NoUnexpectedReceived();
            Object.Destroy(listener);
        }

        [UnityTest]
        public IEnumerator PlaySe_AudioSourceDestroyed_DoesNotPoisonPool()
        {
            var listener = new GameObject("Listener", typeof(AudioListener));
            soundManager.PlaySe(clip);
            foreach (var source in FindPooledAudioSources()) Object.Destroy(source.gameObject);
            yield return null;
            soundManager.Tick();

            ISeHandle next = null;
            Assert.DoesNotThrow(() => next = soundManager.PlaySe(clip));

            Assert.IsTrue(next.IsValid);
            LogAssert.NoUnexpectedReceived();
            Object.Destroy(listener);
        }
    }
}