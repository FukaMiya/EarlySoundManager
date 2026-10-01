# Early Sound Manager

Unity 向けの軽量オーディオ管理パッケージです。

## 機能

- オブジェクトプールによる `AudioSource` 管理
- BGM クロスフェード（複数トラック独立管理）
- SE 再生完了後の自動プール回収
- Spatial Sound（`Transform` 追従による 3D 定位）
- マスター / BGM / SE カテゴリ別音量管理
- 音量・ピッチのフェードアニメーション
- async/await 対応（UniTask / Unity Awaitable / ValueTask）
- VContainer DI 対応（`ITickable` 互換）

---

## インストール

### Unity Package Manager（推奨）

**Window > Package Manager > Add package from git URL** から以下の URL を入力してください。

```
https://github.com/FukaMiya/EarlySoundManager.git?path=Assets/EarlySoundManager
```

### 手動インストール

`Assets/EarlySoundManager` フォルダをプロジェクトの `Assets` 以下にコピーしてください。

---

## Usage

### Initialize

`Tick()` をプレイヤーループに組み込む必要があります。VContainer を導入している場合、`VContainer.Unity.ITickable` と互換性があります。

```csharp
public SoundRegistry soundRegistry;
private SoundManager soundManager;

void Start()
{
    soundManager = new SoundManager(soundRegistry);
}

void Update()
{
    soundManager.Tick();
}
```

`SoundRegistry` を使用しない場合は引数なしで初期化できます。

```csharp
soundManager = new SoundManager();
```

---

### Play SE

SE は再生終了後、自動的にプールへ回収されます。

```csharp
// AudioClip または文字列キーで再生
soundManager.PlaySe(clip);
soundManager.PlaySe("key");
soundManager.PlaySe("key", soundOptions);

// ハンドルによる操作
var handle = soundManager.PlaySe("key");
handle.Pause();
handle.Pause(new SoundFadingOptions(fadeDuration: 0.5f)); // フェードアウトして一時停止
handle.Resume();
handle.Resume(new SoundFadingOptions(fadeDuration: 0.5f)); // 再開してフェードイン
handle.Stop();
handle.Stop(new SoundFadingOptions(fadeDuration: 0.5f));   // フェードアウトして停止
handle.SetVolume(0.5f);
handle.SetVolume(0.5f, new SoundFadingOptions(fadeDuration: 1.0f)); // フェードで音量変更
handle.SetPitch(1.2f);
handle.SetPitch(1.2f, new SoundFadingOptions(fadeDuration: 1.0f));  // フェードでピッチ変更
```

---

### Play BGM

BGM は自動的にループします。`BgmTrackId` によって複数のトラックを独立して管理できます。デフォルトは `BgmTrackId.Main` です。

```csharp
// 再生（すでに同トラックで BGM が再生中の場合は SwitchBgm と同じ動作）
soundManager.PlayBgm(clip);
soundManager.PlayBgm("key");
soundManager.PlayBgm("key", soundOptions);

// トラックを指定して再生
soundManager.PlayBgm("key", BgmTrackId.Ambient);

// 切り替え（即時）
soundManager.SwitchBgm("key");
soundManager.SwitchBgm("key", BgmTrackId.Ambient);

// クロスフェードで切り替え
soundManager.SwitchBgm("key", new SoundFadingOptions(fadeDuration: 1.0f));
soundManager.SwitchBgm("key", soundOptions, new SoundFadingOptions(fadeDuration: 1.0f));
soundManager.SwitchBgm("key", new SoundFadingOptions(fadeDuration: 1.0f), BgmTrackId.Ambient);

// ハンドルによる操作
var handle = soundManager.PlayBgm("key");
handle.SetVolume(0.5f);
handle.SetVolume(0.5f, new SoundFadingOptions(fadeDuration: 1.0f));
handle.Pause();
handle.Pause(new SoundFadingOptions(fadeDuration: 0.5f));
handle.Resume();
handle.Resume(new SoundFadingOptions(fadeDuration: 0.5f));
```

> BGM を完全に停止させることはできません。一時停止するか、別の BGM に切り替えてください。

---

### Async / Await

SE の再生完了や BGM のフェード完了を async/await で待機できます。UniTask が導入されている場合は UniTask、Unity 2023.1 以降では Awaitable、それ以外では ValueTask が使用されます。

#### SE の再生完了を待機

```csharp
// PlaySeAsync — 再生完了まで待機
await soundManager.PlaySeAsync("key");
await soundManager.PlaySeAsync(clip, cancellationToken);

// await handle — ハンドルを直接 await
var handle = soundManager.PlaySe("key");
await handle;
```

#### SE ハンドルのフェード操作を待機

```csharp
var handle = soundManager.PlaySe("key");
await handle.StopAsync(new SoundFadingOptions(fadeDuration: 0.5f));
await handle.PauseAsync(new SoundFadingOptions(fadeDuration: 0.5f));
await handle.ResumeAsync(new SoundFadingOptions(fadeDuration: 0.5f));
await handle.SetVolumeAsync(0f, new SoundFadingOptions(fadeDuration: 1.0f));
await handle.SetPitchAsync(0.5f, new SoundFadingOptions(fadeDuration: 1.0f));
```

#### BGM のクロスフェード完了を待機

```csharp
// SwitchBgmAsync — フェード完了まで待機（result に新しいハンドルが入る）
await soundManager.SwitchBgmAsync("key", new SoundFadingOptions(fadeDuration: 1.0f), out var result);
await soundManager.SwitchBgmAsync("key", soundOptions, new SoundFadingOptions(fadeDuration: 1.0f), out var result);
```

---

### Volume Management

マスター・SE・BGM それぞれの音量を個別に設定できます。各ハンドルの実際の音量は `BaseVolume × カテゴリ音量 × MasterVolume` で計算されます。

```csharp
soundManager.SetMasterVolume(0.8f);
soundManager.SetSeVolume(0.5f);
soundManager.SetBgmVolume(0.7f);
```

---

### Sound Options

再生時に指定できるパラメータです。

| プロパティ | 型 | デフォルト | 説明 |
|---|---|---|---|
| `BaseVolume` | `float` | `1.0` | 基本音量 |
| `BasePitch` | `float` | `1.0` | 基本ピッチ |
| `Spatialize` | `bool` | `false` | 3D 音響の有効化 |
| `Position` | `Vector3` | `default` | 初期再生位置 |
| `PositionSource` | `Transform` | `null` | 追従する Transform |
| `RolloffMode` | `AudioRolloffMode` | `Logarithmic` | 減衰モード |
| `MinDistance` | `float` | `1.0` | 最小距離 |
| `MaxDistance` | `float` | `500.0` | 最大距離 |

拡張メソッドによる流暢な記述が可能です。

```csharp
var options = SoundOptions.Default
    .WithVolume(0.8f)
    .WithPitch(1.2f)
    .WithSpatialization(true)
    .WithPosition(transform.position)
    .WithPositionSource(transform)
    .WithRolloffMode(AudioRolloffMode.Linear)
    .WithMinDistance(2.0f)
    .WithMaxDistance(100.0f);
```

Inspector でシリアライズする場合は `SerializableSoundOptions` を使用してください。`SoundOptions` との暗黙的な型変換に対応しています。

---

### Sound Fading Options

音量・ピッチのフェードや BGM クロスフェードに使用します。

| プロパティ | 型 | デフォルト | 説明 |
|---|---|---|---|
| `FadeDuration` | `float` | — | フェードにかける時間（秒） |
| `CancelBehaviour` | `CancelBehaviour` | `Cancel` | キャンセル時の挙動 |

`CancelBehaviour` の値:

| 値 | 説明 |
|---|---|
| `Cancel` | キャンセル時に待機を中断（例外を投げる） |
| `Complete` | キャンセル時にフェードを即時完了して正常終了 |
| `Ignore` | キャンセルを無視してフェードを継続 |

Inspector でシリアライズする場合は `SerializableSoundFadingOptions` を使用してください。

---

### BGM Tracks

`BgmTrackId` を使って複数の BGM トラックを独立して管理できます。

```csharp
// 定義済みトラック
BgmTrackId.Main    // デフォルトトラック
BgmTrackId.Ambient // アンビエントトラック

// カスタムトラック
var myTrack = new BgmTrackId("MyCustomTrack");
soundManager.PlayBgm("key", myTrack);
```

---

### Sound Registry

`ScriptableObject` でオーディオクリップを文字列キーで管理します。
`Assets > Create > SoundManager > SoundRegistry` からアセットを作成し、`key` と `AudioClip` のペアを登録してください。

---

### ISoundHandle Events

ハンドルにはイベントが用意されており、状態変化を検知できます。

```csharp
var handle = soundManager.PlaySe("key");
handle.OnPaused        += () => Debug.Log("Paused");
handle.OnResumed       += () => Debug.Log("Resumed");
handle.OnVolumeChanged += () => Debug.Log("Volume changed");
handle.OnPitchChanged  += () => Debug.Log("Pitch changed");

var seHandle = soundManager.PlaySe("key");
seHandle.OnCompleted   += () => Debug.Log("SE completed");
```

---

## License

[MIT License](LICENSE)
