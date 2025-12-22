# Claude Code公式ドキュメント徹底調査レポート

## はじめに

このドキュメントは、Claude Codeの公式ドキュメント（2025年版）に基づいた、詳細な機能説明、ベストプラクティス、実装例をまとめたものです。

---

## 1. 計画管理（Plan Mode、TODO管理）

### 1.1 Plan Modeについて

**Plan Mode**は、Claude Codeの読み取り専用分析モードで、実際の変更を加える前にコードベースを調査し、詳細な計画を立てるための機能です。

#### Plan Modeの有効化方法

**セッション中に有効化（推奨）：**
```
Shift+Tab キーを複数回押す
```
ショートカットは以下のサイクルで動作します：
- 通常モード → Auto-Accept Mode（⏵⏵ accept edits on）→ Plan Mode（⏸ plan mode on）→ 通常モード

**新規セッションでPlan Modeを開始：**
```bash
claude --permission-mode plan
```

**ヘッドレスモードでPlan Modeを使用：**
```bash
claude --permission-mode plan -p "Analyze the authentication system and suggest improvements"
```

#### Plan Modeの設定

`.claude/settings.json`でPlan Modeをデフォルトに設定：

```json
{
  "permissions": {
    "defaultMode": "plan"
  }
}
```

#### Plan Mode使用例：複雑なリファクタの計画

```bash
claude --permission-mode plan
```

```
> I need to refactor our authentication system to use OAuth2. Create a detailed migration plan.
```

Claude分析し、包括的な計画を作成します。その後、フォローアップで以下のような質問ができます：

```
> What about backward compatibility?
> How should we handle database migration?
```

### 1.2 TODOリストの管理

Claude Codeは`/todos`コマンドで構造化されたタスクリストを作成・管理できます。

```
> /todos
```

これにより、TodoWriteツールで以下が可能になります：
- 現在のTODO項目の表示
- タスクの進捗追跡
- タスク完了時の自動更新

### 1.3 セッション名の管理

複数のタスクを並行して管理する場合、セッションに名前をつけることが重要です：

```
> /rename auth-refactor
```

後で再開する際：
```bash
claude --resume auth-refactor
```

---

## 2. ワークフロー（推奨開発フロー）

### 2.1 Anthropic推奨の開発フロー

Claude Codeの推奨フローは、以下のパターンに従います：

#### パターン1：Explore → Plan → Code → Test → Commit

**1. Explore（調査フェーズ）**

新しいコードベースを理解する：
```
> give me an overview of this codebase
```

```
> explain the main architecture patterns used here
```

```
> what are the key data models?
```

**2. Plan（計画フェーズ）**

複雑な変更の場合、Plan Modeを使用：
```bash
claude --permission-mode plan
```

```
> I need to add user authentication to the API. Create a detailed plan.
```

Plan Modeでは読み取り専用なので、Claude Codeは以下を行います：
- ファイル構造を探索
- 既存の認証方法を調査
- 実装計画を提案

**3. Code（実装フェーズ）**

Plan が承認されたら、通常モードで実装：
```bash
claude
```

```
> Now implement the authentication system according to the plan
```

**4. Test（テストフェーズ）**

実装後、テストを実行：
```
> run the tests
> add tests for the new authentication endpoints
> fix any test failures
```

**5. Commit（コミットフェーズ）**

変更をコミット：
```
> commit my changes with a descriptive message
```

or

```bash
claude commit
```

### 2.2 バグ修正フロー

**1. エラーを共有**
```
> I'm seeing an error when I run npm test
```

**2. 修正案を得る**
```
> suggest a few ways to fix this issue
```

**3. 修正を実装**
```
> update user.ts to add the null check you suggested
```

**4. テストして確認**
```
> run tests and verify the fix
```

### 2.3 リファクタリングフロー

**1. レガシーコードを特定**
```
> find deprecated API usage in our codebase
```

**2. リファクタリング推奨を得る**
```
> suggest how to refactor utils.js to use modern JavaScript features
```

**3. 安全に変更を適用**
```
> refactor utils.js to use ES2024 features while maintaining the same behavior
```

**4. リファクタリングを検証**
```
> run tests for the refactored code
```

---

## 3. CLAUDE.md：効果的な使い方

### 3.1 CLAUDE.mdの役割

`CLAUDE.md`は、Claude Codeが自動的に読み込むメモリファイルで、プロジェクトに関する指示、ベストプラクティス、コードスタイル、頻繁に使うコマンドなどを記録します。

### 3.2 メモリの階層構造

Claude Codeは4つのメモリレベルをサポートしています（優先度順）：

| メモリタイプ | 場所 | 目的 | 共有範囲 |
|---|---|---|---|
| **Enterprise policy** | `C:\Program Files\ClaudeCode\CLAUDE.md` (Windows) | 組織全体の指示 | 組織全員 |
| **Project memory** | `./CLAUDE.md` or `./.claude/CLAUDE.md` | チーム共有の指示 | ソースコントロール経由でチーム共有 |
| **Project rules** | `./.claude/rules/*.md` | トピック別モジュール指示 | ソースコントロール経由でチーム共有 |
| **User memory** | `~/.claude/CLAUDE.md` | 個人の全プロジェクト共通設定 | 個人のみ |
| **Project local memory** | `./CLAUDE.local.md` | プロジェクト特有の個人設定 | 個人のみ（自動.gitignore） |

### 3.3 効果的なCLAUDE.mdの内容

#### 推奨される内容（ベストプラクティス）

1. **よく使うコマンド**
```markdown
# Project Commands

## Build and Test
- Build: `npm run build`
- Test: `npm run test`
- Lint: `npm run lint`
- Dev server: `npm run dev`
```

2. **コーディング規約**
```markdown
# Code Style

- Use 2-space indentation
- Prefer const over let
- Always add JSDoc comments to public functions
- Use TypeScript strict mode
- Prefer async/await over promises
```

3. **アーキテクチャパターン**
```markdown
# Architecture

## Project Structure
- `/src/components`: React components
- `/src/services`: API and business logic
- `/src/utils`: Utility functions
- `/tests`: Test files
```

### 3.4 モジュール化ルール（.claude/rules/）

大規模プロジェクトでは、ルールを複数のファイルに分割：

```
your-project/
├── .claude/
│   ├── CLAUDE.md           # メインのプロジェクト指示
│   └── rules/
│       ├── code-style.md   # コードスタイルガイドライン
│       ├── testing.md      # テスト規約
│       ├── api.md          # API開発ルール
│       └── security.md     # セキュリティ要件
```

#### パス固有ルール

特定のファイルパターンに適用するルール：

```markdown
---
paths: src/api/**/*.ts
---

# API Development Rules

- All API endpoints must include input validation
- Use the standard error response format
- Include OpenAPI documentation comments
```

---

## 4. ベストプラクティス（Anthropic内部での使用例）

### 4.1 Subagents（サブエージェント）の活用

複雑なワークフローでは、特化したSubagentsを使用します。

#### 利用可能なビルトインSubagents

**1. General-purpose Subagent**
- 複雑な複数ステップタスク
- 調査と変更の両方が必要
- Sonnetを使用

**2. Plan Subagent**
- Plan Modeで自動使用
- 読み取り専用の調査
- Sonnetを使用

**3. Explore Subagent**
- 軽量で高速な検索
- 厳密な読み取り専用
- Haikuを使用（低レイテンシ）

### 4.2 Git Worktreesを使った並行処理

Team環境での複数タスク並行処理：

```bash
# main ブランチで新機能用worktreeを作成
git worktree add ../proj-feature-auth -b feature/auth

# バグ修正用worktreeを作成
git worktree add ../proj-bugfix-123 bugfix/issue-123

# それぞれで独立したClaudeセッションを実行
cd ../proj-feature-auth
claude

# 別ターミナルで
cd ../proj-bugfix-123
claude
```

### 4.3 Hooksを使った自動化

ファイル変更時に自動的にフォーマットを実行：

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [
          {
            "type": "command",
            "command": "npx prettier --write \"${FILE_PATH}\" || true"
          }
        ]
      }
    ]
  }
}
```

---

## 5. TDD（テスト駆動開発）の進め方

### 5.1 TDD推奨フロー（テスト先行）

1. **テストを書く**
   ```
   > write a test for the user registration form validation
   ```

2. **テストが失敗することを確認**
   ```
   > run the tests and verify they fail
   ```

3. **実装を追加**
   ```
   > implement the user registration form validation to make the tests pass
   ```

4. **テストをパス**
   ```
   > run the tests again
   ```

5. **リファクタ**
   ```
   > refactor the implementation to improve code quality
   ```

---

## 6. 複雑なタスクの分解

### 6.1 大きなタスクをPlan Modeで分解

```bash
claude --permission-mode plan
```

```
> Help me migrate our authentication system from JWT to OAuth2. Break this down into phases.
```

Claude が返す内容：
- 各フェーズの詳細な手順
- 依存関係
- 潜在的な問題
- テスト戦略

### 6.2 段階的な実装

Plan が完成したら、各段階を実装：

```
> Phase 1: Set up OAuth2 provider configuration
> Phase 2: Update the authentication service
> Phase 3: Migrate existing user sessions
> Phase 4: Add logout and refresh token handling
```

---

## 7. 設定とカスタマイズ

### 7.1 settings.jsonの構造

設定は階層的に保存されます：

| スコープ | 場所 | 共有 | 優先度 |
|---|---|---|---|
| Enterprise | `C:\Program Files\ClaudeCode\managed-settings.json` | 全員 | 最高 |
| User | `~/.claude/settings.json` | 個人 | 中 |
| Project | `.claude/settings.json` | ソースコントロール | 低 |
| Local | `.claude/settings.local.json` | 個人のみ | 最低 |

### 7.2 重要な設定オプション

#### パーミッション設定

```json
{
  "permissions": {
    "allow": [
      "Bash(npm run:*)",
      "Bash(git:*)",
      "Read(src/**)"
    ],
    "deny": [
      "Bash(curl:*)",
      "Bash(rm:*)",
      "Read(.env)"
    ],
    "ask": [
      "Bash(git push:*)"
    ],
    "defaultMode": "acceptEdits"
  }
}
```

#### フック設定

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [
          {
            "type": "command",
            "command": "npx prettier --write \"${FILE_PATH}\""
          }
        ]
      }
    ]
  }
}
```

---

## 8. スラッシュコマンド完全リファレンス

### 8.1 ビルトインコマンド一覧

| コマンド | 目的 |
|---|---|
| `/add-dir` | 作業ディレクトリを追加 |
| `/agents` | Subagent管理 |
| `/clear` | 会話履歴クリア |
| `/compact` | 会話をコンパクト化 |
| `/config` | 設定UI |
| `/context` | コンテキスト使用可視化 |
| `/cost` | トークン使用統計 |
| `/doctor` | インストール状態確認 |
| `/help` | ヘルプ表示 |
| `/hooks` | フック設定 |
| `/init` | CLAUDE.md初期化 |
| `/memory` | メモリファイル編集 |
| `/model` | モデル選択 |
| `/permissions` | パーミッション表示 |
| `/rename <name>` | セッション名変更 |
| `/resume` | セッション再開 |
| `/todos` | TODO表示 |

### 8.2 カスタムスラッシュコマンド

#### プロジェクトコマンドの作成

`.claude/commands/optimize.md`:

```markdown
Analyze this code for performance issues and suggest three specific optimizations:
```

使用方法：
```
> /optimize
```

#### 引数を使ったコマンド

`.claude/commands/fix-issue.md`:

```markdown
Find and fix issue #$ARGUMENTS. Follow these steps:
1. Understand the issue described in the ticket
2. Locate the relevant code
3. Implement a solution addressing the root cause
4. Add appropriate tests
5. Prepare a PR description
```

使用方法：
```
> /fix-issue 456
```

---

## 9. キーボードショートカット

| ショートカット | 動作 |
|---|---|
| `Ctrl+C` | 入力・生成キャンセル |
| `Ctrl+D` | Claude Code終了 |
| `Ctrl+L` | 画面クリア |
| `Ctrl+R` | コマンド履歴逆検索 |
| `↑ / ↓` | コマンド履歴ナビゲート |
| `Esc + Esc` | 変更を戻す |
| `Shift+Tab` | パーミッションモード切り替え |
| `#` (先頭) | メモリショートカット（CLAUDE.mdへ追加） |
| `/` (先頭) | スラッシュコマンド |
| `!` (先頭) | Bashモード（直接Bash実行） |
| `@` | ファイルパス記述（オートコンプリート） |

---

## まとめ

Claude Code を最も効果的に使用するには：

1. **CLAUDE.md を充実させる** - チーム共通のコンテキストを蓄積
2. **Plan Mode で計画する** - 大きな変更は事前分析
3. **Subagents を活用する** - 専門化した AI エージェントに任せる
4. **Hooks で自動化** - 繰り返しタスクを自動実行
5. **TDD を実践する** - テスト先行で品質を担保

**推奨ワークフロー:**
```
Explore → Plan → Code → Test → Commit
```
