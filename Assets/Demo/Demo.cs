using UnityEngine;
using Early.SoundManager;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public sealed class Demo : MonoBehaviour
{
    [SerializeField] private SoundRegistry SoundRegistry;

    private SoundManager soundService;
    private IBgmHandle currentBgm;
    private ISeHandle lastSe;

    private string status = "Ready";
    private CancellationTokenSource cts = new();

    // GUI styles (initialized on first OnGUI call)
    private GUIStyle styleLabel;
    private GUIStyle styleHeader;
    private GUIStyle styleButton;
    private GUIStyle styleCancelButton;

    private void InitStyles()
    {
        if (styleLabel != null) return;

        styleLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            wordWrap = true,
        };

        styleHeader = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
        };

        styleButton = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
        };

        styleCancelButton = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
        };
    }

    private float fadeDuration = 1.5f;
    private float targetVolume = 0.3f;
    private float masterVolume = 1f;
    private float seVolume = 1f;
    private float bgmVolume = 1f;

    // CancelBehaviour.Complete を使うことで、キャンセル時にフェードが即終端値に飛ぶ挙動を示す
    private SoundFadingOptions FadingOptions => new(fadeDuration, CancelBehaviour.Complete);

    private string BgmStatusText =>
        currentBgm is { IsValid: true, IsPlaying: true } ? "Playing" :
        currentBgm is { IsValid: true, IsPaused: true } ? "Paused" :
        "Stopped";

    private string SeStatusText =>
        lastSe is { IsValid: true, IsPlaying: true } ? "Playing" :
        lastSe is { IsValid: true, IsPaused: true } ? "Paused" :
        "Idle";

    // -------------------------------------------------------

    private void Start()
    {
        soundService = new SoundManager(SoundRegistry);
    }

    private void Update()
    {
        soundService.Tick();
    }

    private void OnDestroy()
    {
        cts.Cancel();
        cts.Dispose();
        soundService.Dispose();
    }

    // -------------------------------------------------------

    private void OnGUI()
    {
        InitStyles();

        const int btnH    = 52;   // multi-line button height
        const int btnH1   = 40;   // single-line button height
        const int sliderH = 24;   // slider thumb height

        GUILayout.BeginArea(new Rect(10, 10, 620, Screen.height - 20));
        GUILayout.BeginVertical(GUI.skin.box);

        // --- State display ---
        GUILayout.Label($"Status : {status}", styleLabel);
        GUILayout.Label($"BGM    : {BgmStatusText}  Vol={currentBgm?.Volume:F2}  Base={currentBgm?.BaseVolume:F2}", styleLabel);
        GUILayout.Label($"Last SE: {SeStatusText}", styleLabel);
        GUILayout.Space(10);

        // --- Fade parameters ---
        GUILayout.Label($"Fade Duration : {fadeDuration:F2} s  (CancelBehaviour = Complete)", styleLabel);
        fadeDuration = GUILayout.HorizontalSlider(fadeDuration, 0.1f, 5f, GUILayout.Height(sliderH));
        GUILayout.Label($"Target Volume : {targetVolume:F2}", styleLabel);
        targetVolume = GUILayout.HorizontalSlider(targetVolume, 0f, 1f, GUILayout.Height(sliderH));
        GUILayout.Space(6);

        if (GUILayout.Button("★ Cancel current async operation", styleCancelButton, GUILayout.Height(btnH1)))
        {
            cts.Cancel();
            cts.Dispose();
            cts = new CancellationTokenSource();
            status = "Cancelled";
        }

        // --- SE ---
        GUILayout.Space(10);
        GUILayout.Label("─── Sound Effects ───", styleHeader);

        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Play Se1\n(fire & forget)",  styleButton, GUILayout.Height(btnH))) PlaySe("Se1");
            if (GUILayout.Button("Play Se2\n(fire & forget)",  styleButton, GUILayout.Height(btnH))) PlaySe("Se2");
            if (GUILayout.Button("Play Se3\n(fire & forget)",  styleButton, GUILayout.Height(btnH))) PlaySe("Se3");
        }

        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Play Se1\n(await completion)", styleButton, GUILayout.Height(btnH))) PlaySeAndAwaitAsync("Se1").Forget();
            if (GUILayout.Button("Play Se2\n(await completion)", styleButton, GUILayout.Height(btnH))) PlaySeAndAwaitAsync("Se2").Forget();
        }

        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Stop last SE\n(await fade)",   styleButton, GUILayout.Height(btnH))) StopLastSeAsync().Forget();
            if (GUILayout.Button("Pause last SE\n(await fade)",  styleButton, GUILayout.Height(btnH))) PauseLastSeAsync().Forget();
            if (GUILayout.Button("Resume last SE\n(await fade)", styleButton, GUILayout.Height(btnH))) ResumeLastSeAsync().Forget();
        }

        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("SetVolume→Target\n(await fade)", styleButton, GUILayout.Height(btnH))) SetLastSeVolumeAsync(targetVolume).Forget();
            if (GUILayout.Button("SetPitch→1.5\n(await fade)",     styleButton, GUILayout.Height(btnH))) SetLastSePitchAsync(1.5f).Forget();
            if (GUILayout.Button("SetPitch→1.0\n(await fade)",     styleButton, GUILayout.Height(btnH))) SetLastSePitchAsync(1.0f).Forget();
        }

        // --- BGM ---
        GUILayout.Space(10);
        GUILayout.Label("─── Background Music ───", styleHeader);

        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("SwitchBgm1\n(await crossfade)", styleButton, GUILayout.Height(btnH))) SwitchBgmAndAwaitAsync("Bgm1").Forget();
            if (GUILayout.Button("SwitchBgm2\n(await crossfade)", styleButton, GUILayout.Height(btnH))) SwitchBgmAndAwaitAsync("Bgm2").Forget();
        }

        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Pause BGM\n(await fade)",     styleButton, GUILayout.Height(btnH))) PauseBgmAsync().Forget();
            if (GUILayout.Button("Resume BGM\n(await fade-in)", styleButton, GUILayout.Height(btnH))) ResumeBgmAsync().Forget();
        }

        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("SetVolume→Target\n(await fade)", styleButton, GUILayout.Height(btnH))) SetBgmVolumeAsync(targetVolume).Forget();
            if (GUILayout.Button("SetVolume→1.0\n(await fade)",    styleButton, GUILayout.Height(btnH))) SetBgmVolumeAsync(1f).Forget();
            if (GUILayout.Button("SetPitch→1.5\n(await fade)",     styleButton, GUILayout.Height(btnH))) SetBgmPitchAsync(1.5f).Forget();
            if (GUILayout.Button("SetPitch→1.0\n(await fade)",     styleButton, GUILayout.Height(btnH))) SetBgmPitchAsync(1.0f).Forget();
        }

        // --- Volume ---
        GUILayout.Space(10);
        GUILayout.Label("─── Volume ───", styleHeader);
        DrawVolumeSlider("Master", ref masterVolume, soundService.SetMasterVolume, sliderH);
        DrawVolumeSlider("SE    ", ref seVolume,     soundService.SetSeVolume,     sliderH);
        DrawVolumeSlider("BGM   ", ref bgmVolume,    soundService.SetBgmVolume,    sliderH);

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawVolumeSlider(string label, ref float value, System.Action<float> setter, int sliderH)
    {
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label($"{label}: {value:F2}", styleLabel, GUILayout.Width(150));
            float next = GUILayout.HorizontalSlider(value, 0f, 1f, GUILayout.Height(sliderH));
            if (!Mathf.Approximately(next, value))
            {
                value = next;
                setter(value);
            }
        }
    }

    // -------------------------------------------------------
    // SE
    // -------------------------------------------------------

    private void PlaySe(string key)
    {
        lastSe = soundService.PlaySe(key);
        status = $"[{key}] playing (fire & forget)";
    }

    private async UniTask PlaySeAndAwaitAsync(string key)
    {
        lastSe = soundService.PlaySe(key);
        status = $"[{key}] playing... (awaiting completion)";
        try
        {
            await lastSe.ToUniTask(cts.Token);
            status = $"[{key}] playback completed";
        }
        catch (OperationCanceledException)
        {
            status = $"[{key}] await cancelled";
        }
    }

    private async UniTask StopLastSeAsync()
    {
        if (lastSe is not { IsValid: true }) { status = "No active SE"; return; }
        status = "SE stopping... (await fade)";
        try
        {
            await lastSe.StopAsync(FadingOptions, cts.Token);
            status = "SE stopped";
        }
        catch (OperationCanceledException) { status = "SE stop cancelled"; }
    }

    private async UniTask PauseLastSeAsync()
    {
        if (lastSe is not { IsValid: true, IsPlaying: true }) { status = "SE not playing"; return; }
        status = "SE pausing... (await fade)";
        try
        {
            await lastSe.PauseAsync(FadingOptions, cts.Token);
            status = "SE paused";
        }
        catch (OperationCanceledException) { status = "SE pause cancelled"; }
    }

    private async UniTask ResumeLastSeAsync()
    {
        if (lastSe is not { IsValid: true, IsPaused: true }) { status = "SE not paused"; return; }
        status = "SE resuming... (await fade-in)";
        try
        {
            await lastSe.ResumeAsync(FadingOptions, cts.Token);
            status = "SE resumed";
        }
        catch (OperationCanceledException) { status = "SE resume cancelled"; }
    }

    private async UniTask SetLastSeVolumeAsync(float volume)
    {
        if (lastSe is not { IsValid: true }) { status = "No active SE"; return; }
        status = $"SE volume → {volume:F2}... (await fade)";
        try
        {
            await lastSe.SetVolumeAsync(volume, FadingOptions, cts.Token);
            status = $"SE volume set to {volume:F2}";
        }
        catch (OperationCanceledException) { status = "SE SetVolume cancelled"; }
    }

    private async UniTask SetLastSePitchAsync(float pitch)
    {
        if (lastSe is not { IsValid: true }) { status = "No active SE"; return; }
        status = $"SE pitch → {pitch:F2}... (await fade)";
        try
        {
            await lastSe.SetPitchAsync(pitch, FadingOptions, cts.Token);
            status = $"SE pitch set to {pitch:F2}";
        }
        catch (OperationCanceledException) { status = "SE SetPitch cancelled"; }
    }

    // -------------------------------------------------------
    // BGM
    // -------------------------------------------------------

    private async UniTask SwitchBgmAndAwaitAsync(string key)
    {
        status = $"[{key}] switching... (awaiting crossfade)";
        try
        {
            await soundService.SwitchBgmAsync(key, FadingOptions, out currentBgm, cancellationToken: cts.Token);
            status = $"[{key}] crossfade complete";
        }
        catch (OperationCanceledException) { status = $"[{key}] switch cancelled"; }
    }

    private async UniTask PauseBgmAsync()
    {
        if (currentBgm is not { IsValid: true }) { status = "No BGM"; return; }
        status = "BGM pausing... (await fade)";
        try
        {
            await currentBgm.PauseAsync(FadingOptions, cts.Token);
            status = "BGM paused";
        }
        catch (OperationCanceledException) { status = "BGM pause cancelled"; }
    }

    private async UniTask ResumeBgmAsync()
    {
        if (currentBgm is not { IsValid: true }) { status = "No BGM"; return; }
        status = "BGM resuming... (await fade-in)";
        try
        {
            await currentBgm.ResumeAsync(FadingOptions, cts.Token);
            status = "BGM resumed";
        }
        catch (OperationCanceledException) { status = "BGM resume cancelled"; }
    }

    private async UniTask SetBgmVolumeAsync(float volume)
    {
        if (currentBgm is not { IsValid: true }) { status = "No BGM"; return; }
        status = $"BGM volume → {volume:F2}... (await fade)";
        try
        {
            await currentBgm.SetVolumeAsync(volume, FadingOptions, cts.Token);
            status = $"BGM volume set to {volume:F2}";
        }
        catch (OperationCanceledException) { status = "BGM SetVolume cancelled"; }
    }

    private async UniTask SetBgmPitchAsync(float pitch)
    {
        if (currentBgm is not { IsValid: true }) { status = "No BGM"; return; }
        status = $"BGM pitch → {pitch:F2}... (await fade)";
        try
        {
            await currentBgm.SetPitchAsync(pitch, FadingOptions, cts.Token);
            status = $"BGM pitch set to {pitch:F2}";
        }
        catch (OperationCanceledException) { status = "BGM SetPitch cancelled"; }
    }
}
