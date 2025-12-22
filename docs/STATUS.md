# Promptveil - 実装状況レポート

## 概要

Windows Terminal上での日本語IME表示崩れを解決するトレイ常駐型WPFアプリケーション。

## 現在の状況: トレイアイコン表示問題

### ビルド状態
- **ビルド**: 成功
- **実行**: プロセス起動確認済み (tasklist で確認)
- **問題**: タスクトレイにアイコンが表示されない

### 調査結果

H.NotifyIcon.Wpf v2.1.3 でのアイコン設定に問題あり:

| アプローチ | 結果 |
|-----------|------|
| `IconSource` + `RenderTargetBitmap` | NotImplementedException |
| `IconSource` + `DrawingImage` | NotImplementedException |
| `IconSource` + `BitmapImage` (from stream) | NullReferenceException (UriSource が null) |
| `Icon` + `System.Drawing.Icon` | ビルド成功、プロセス起動、アイコン表示されず |

### 考えられる原因

1. **H.NotifyIcon.Wpf の仕様**: `Icon` プロパティより `IconSource` を期待している可能性
2. **Windows 11 の通知領域**: オーバーフロー領域に隠れている可能性
3. **アイコンサイズ/形式**: 16x16 の動的生成アイコンが正しく認識されていない可能性

---

## 実装済みファイル

```
c:\Promptveil\
├── Promptveil.csproj           # .NET 8 WPF プロジェクト
├── app.manifest                # Per-Monitor DPI V2
├── src/
│   ├── App.xaml                # トレイメニュー定義
│   ├── App.xaml.cs             # メインアプリケーション (トレイアイコン問題あり)
│   ├── Windows/
│   │   ├── MaskWindow.xaml/.cs         # クリック透過マスク
│   │   ├── InputWindow.xaml/.cs        # IME対応入力オーバーレイ
│   │   └── CalibrationWindow.xaml/.cs  # キャリブレーションUI
│   ├── Services/
│   │   ├── ConfigService.cs            # JSON設定永続化
│   │   ├── ClipboardInjector.cs        # クリップボード貼り付け
│   │   ├── WindowTracker.cs            # ターミナル追従
│   │   └── GlobalHotkeyService.cs      # グローバルホットキー
│   ├── Models/
│   │   └── Config.cs                   # 設定モデル
│   └── Helpers/
│       └── NativeMethods.cs            # Win32 P/Invoke (64bit対応済み)
└── docs/
    ├── PLAN.md                         # 開発計画
    └── STATUS.md                       # このファイル
```

---

## 機能実装状況

| 機能 | 状態 | 備考 |
|-----|------|------|
| プロジェクト構造 | ✅ 完了 | .NET 8 WPF |
| Per-Monitor DPI V2 | ✅ 完了 | app.manifest |
| P/Invoke (64bit対応) | ✅ 完了 | GetWindowLongPtr/SetWindowLongPtr |
| MaskWindow (クリック透過) | ✅ 完了 | WS_EX_TRANSPARENT |
| InputWindow (IME入力) | ✅ 完了 | TextBox + IME設定 |
| CalibrationWindow | ✅ 完了 | 矢印キー調整 |
| ClipboardInjector | ✅ 完了 | バックアップ/リストア |
| WindowTracker | ✅ 完了 | WinEventHook + ポーリングフォールバック |
| GlobalHotkeyService | ✅ 完了 | Ctrl+Alt+C |
| ConfigService | ✅ 完了 | %AppData%/terminal_input_overlay/ |
| コマンド履歴 | ✅ 完了 | Ctrl+Up/Down |
| ホバー閉じボタン | ✅ 完了 | マウスホバーで表示 |
| **トレイアイコン** | ❌ 問題あり | 表示されない |

---

## 次のアクション

### 選択肢 1: .ico ファイルを使用
実際の .ico ファイルを作成してプロジェクトに埋め込み、`pack://` URI で参照する。

```xml
<tb:TaskbarIcon IconSource="pack://application:,,,/Resources/TrayIcon.ico" />
```

### 選択肢 2: 別のトレイライブラリを検討
- **Hardcodet.NotifyIcon.Wpf** (H.NotifyIcon の元版)
- **Windows Forms NotifyIcon** をホスト

### 選択肢 3: Windows 11 通知領域を確認
タスクバーの「^」ボタンでオーバーフロー領域を確認。

---

## ビルドと実行

```bash
cd c:\Promptveil
dotnet build
dotnet run
```

または

```bash
.\bin\Debug\net8.0-windows\Promptveil.exe
```

---

## 設定ファイル

`%AppData%\terminal_input_overlay\config.json`

```json
{
  "target_process": "WindowsTerminal",
  "offset_x": 10,
  "offset_y": -60,
  "mask_lines": 2,
  "line_height_px": 20,
  "input_height_px": 28,
  "font_size": 14,
  "send_empty_enter": false,
  "paste_delay_ms": 50,
  "poll_interval_ms": 50,
  "max_history": 100,
  "history": [],
  "terminal_class": "CASCADIA_HOSTING_WINDOW_CLASS"
}
```

---

*最終更新: 2025-12-22*
