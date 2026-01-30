# PromptVeil

Windows Terminal 上で日本語 IME を使用する際に発生する表示崩れ（文字化け・描画乱れ）を解消するための常駐型オーバーレイツールです。

## 概要

Windows Terminal は日本語 IME の変換候補やインライン入力の描画に問題を抱えることがあります。PromptVeil はターミナルの入力行の上に GUI テキストボックスをオーバーレイ表示し、そこで IME 入力を行った後、クリップボード経由でターミナルにテキストを送信します。

## 機能

- タスクトレイ常駐（システムトレイアイコン）
- Windows Terminal の位置・サイズに追従するオーバーレイウィンドウ
- IME 対応テキストボックスによる日本語入力
- クリップボード経由のテキスト送信（Ctrl+V ペースト方式）
- キャリブレーション機能（Ctrl+Alt+C で入力行位置を調整）
- マスクレイヤー（入力行を隠す黒帯、1〜5行分で設定可能）
- DPI 対応（100%〜150%）
- 単一インスタンス制御（多重起動防止）

## 動作環境

- Windows 10 / 11
- .NET 8.0
- Windows Terminal
- Microsoft IME / Google 日本語入力

## ビルド

```bash
dotnet build
```

## 実行

```bash
dotnet run
```

またはビルド後の `Promptveil.exe` を直接実行してください。

## 使い方

1. `Promptveil.exe` を起動するとタスクトレイに常駐します
2. Windows Terminal を前面に表示します
3. **Ctrl+Alt+C** でキャリブレーションモードに入り、矢印キーでオーバーレイの位置を入力行に合わせます
4. オーバーレイのテキストボックスに日本語を入力し、Enter で送信します

## ライセンス

[MIT License](LICENSE)
