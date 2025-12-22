# Terminal Input Overlay for Windows Terminal
## Specification v1.0 (Hover-close / Tray-resident)

---

## 1. Purpose / Concept

Windows Terminal 上で CLI（codex / claude / shell 等）を使用する際に発生する
**日本語 IME の変換表示崩れ問題**を根本的に回避する。

ターミナルの入力行（`>` 付近）を **黒塗りで不可視化**し、
その上に **GUI入力オーバーレイを正確に重ねる**ことで、

- IME は GUI 側で安定表示
- CLI 操作感は通常のターミナルと同等
- ユーザーは「補助UIの存在を意識しない」

という体験を提供する。

本ツールは **常駐（タスクトレイ）型**とし、  
「入力の主導権は常にオーバーレイ側が持つ」設計とする。

---

## 2. Target Environment

- OS: Windows 10 / 11
- Terminal:
  - Windows Terminal（Microsoft Store版 / 標準版）
- Shell / CLI:
  - PowerShell / cmd / WSL
  - codex / claude など任意のCLI
- IME:
  - Microsoft IME
  - Google日本語入力
- DPI:
  - 100%〜150%（Per-monitor DPI aware 推奨）

---

## 3. Core UX Requirements

### 3.1 Overlay ownership model

- **ターミナル入力行は信頼しない**
- 入力の「正」は常にオーバーレイが保持する
- ターミナルは *表示と実行のための受信先* に過ぎない

---

### 3.2 Visual layout

#### 3.2.1 Mask (blackout)

- ターミナルの入力行を覆う **完全不透明の黒塗り矩形**を表示する
- デフォルト高さ：
  - 2 行分（px指定に内部変換）
- 設定で 1〜5 行分に変更可能
- マウス操作は **クリック透過**（input stealing防止）

#### 3.2.2 Input overlay

- 黒塗り領域の上に **GUI入力欄**を配置
- デフォルト：
  - 単一行入力
  - CLIに近いフォントサイズ
- オプション：
  - 複数行モード（Shift+Enterで改行、Enterで送信）

#### 3.2.3 Prompt appearance

- 入力欄の左側に `>` を描画（装飾）
- 実際のターミナルの `>` とは無関係
- 見た目の一体感のみを目的とする

---

### 3.3 Hover close (×) behavior

- **通常時は × ボタンを表示しない**
- オーバーレイ右上にマウスカーソルが入った時のみ：
  - 半透明の × ボタンを表示
- × の動作：
  - オーバーレイを非表示（hide）
  - **アプリ終了はしない**
- 視認性要件：
  - 入力中・通常操作の邪魔にならない
  - ホバーが外れたら即非表示

---

### 3.4 Keyboard interaction

#### 3.4.1 Input & send

- Enter 押下時：
  1. オーバーレイ入力文字列を取得
  2. Windows Terminal をアクティブ化（必要時）
  3. 文字列を **貼り付け（Ctrl+V）** で送信
  4. 続けて Enter を送信（CLI実行）
  5. 入力欄をクリア
  6. フォーカスをオーバーレイへ戻す

- 入力が空の場合：
  - デフォルト：何もしない
  - 設定で「空 Enter も送信」を選択可能

#### 3.4.2 Editing & history

- Ctrl+Up / Ctrl+Down：
  - 履歴を前後に展開し、編集可能
- Esc：
  - 入力欄クリア（非表示にはしない）
- Ctrl+L：
  - 入力欄クリア
- （任意）Ctrl+R：
  - 履歴検索（v1.1以降可）

---

## 4. Calibration (Manual Alignment)

### 4.1 Purpose

Windows Terminal は入力行のピクセル座標を外部に公開しないため、
**1回の手動キャリブレーションで `>` 位置を特定**する。

---

### 4.2 Calibration mode

- ホットキー：Ctrl+Alt+C
- モード中：
  - オーバーレイは半透明
  - 基準点マーカーを表示
  - 現在の (dx, dy) を画面表示

#### 操作

- Alt + 矢印：1px 移動
- Shift + Alt + 矢印：10px 移動
- Enter：確定・保存
- Esc：キャンセル

---

### 4.3 Persistence

- 保存先：
  - %AppData%/terminal_input_overlay/config.json
- 保存内容：
  - target_process = "WindowsTerminal.exe"
  - dx, dy（ウィンドウ左上基準）
  - mask_height_px
  - input_height_px
  - optional: font_scale_hint

---

## 5. Auto-follow behavior

- Windows Terminal の
  - 移動
  - リサイズ
に追従してオーバーレイも移動する

### 5.1 Implementation

- Terminal window rect を取得
- rect + (dx, dy) → overlay position
- 更新方法：
  - ポーリング（30〜60ms）
  - または WinEventHook（可能なら）

---

## 6. Tray-resident behavior

### 6.1 General

- 本アプリは **常にタスクトレイ常駐**
- メインUIは存在しない（オーバーレイのみ）

### 6.2 Tray menu

右クリックメニュー：

- Toggle Overlay
- Calibration...
- Pause Overlay
- Settings
- Reload Config
- Quit

### 6.3 Exit semantics

- × ボタン：
  - 非表示のみ
- アプリ終了：
  - **Tray → Quit のみ**

---

## 7. Focus & injection safety

- クリップボード方式を最優先
- 送信前に既存クリップボードを退避
- 送信後に復元
- 貼り付け後、20〜60ms 待機してから Enter
- 失敗時は 1 回だけリトライ可

---

## 8. Multi-monitor / DPI

- Per-monitor DPI aware を推奨
- DPI変更時：
  - キャリブレーションずれ検知 → 再調整を通知
- モニタ跨ぎ移動時も追従すること

---

## 9. Known limitations

- フォントサイズ / padding / タブバー変更でズレる可能性あり
- ペイン分割時は保証外（ただし追従は試みる）
- vim / less 等のフルスクリーンTUI時は：
  - 手動 Pause を推奨

---

## 10. Acceptance Criteria

- 日本語 IME 変換中表示が崩れない
- 入力体験が「通常のCLIと同等」に感じられる
- キャリブレーション後、`>` に自然に重なる
- ターミナル移動・リサイズに追従する
- × が通常時に視界に入らない
- 終了事故が起きない（常駐前提）

---

