# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Language

- **Internal documents**: English (code, comments, CLAUDE.md, docs)
- **UI**: Japanese (menus, messages, error displays)
- **User interaction**: Japanese (conversations, explanations, answers)
- **Thinking**: English (Claude's internal processing)

## Context Management

Context window is finite. Prevent pollution:

- **Delegate to Subagents** for research, review, debugging (they have isolated context)
- **Use `/compact`** proactively before context fills up
- **Split tasks** into focused sessions with `/clear`
- **Check usage** with `/context`

### Subagents (in `.claude/agents/`)

| Agent | Use When |
|-------|----------|
| `wpf-researcher` | Investigating WPF/Win32/P/Invoke patterns |
| `code-reviewer` | After implementing features, before commit |
| `debugger` | Runtime errors, crashes, unexpected behavior |

Subagents run in isolated context - use them liberally to keep main conversation clean.

## Project Overview

Terminal Input Overlay for Windows Terminal - A tray-resident Windows application that solves Japanese IME display corruption by overlaying a GUI input field on the terminal's input line.

## Key Concepts

- **Overlay Ownership Model**: Overlay owns all input state; terminal is display/execution target only
- **Mask Layer**: Black opaque rectangle covering 2 lines (configurable 1-5), click-through
- **Clipboard Injection**: Text sent via Ctrl+V paste (with backup/restore) + Enter

## Target Environment

- Windows 10/11, Windows Terminal
- Microsoft IME / Google Japanese Input
- DPI: 100%-150% (per-monitor DPI aware)

## Architecture

### Calibration: Ctrl+Alt+C hotkey, arrow keys for adjustment
### Window Tracking: Polling (30-60ms) or WinEventHook
### Config: `%AppData%/terminal_input_overlay/config.json`

## Feature Addition Triggers

Claude MUST propose adding these when conditions are met:

| Feature | Trigger | File |
|---------|---------|------|
| `/build` | First successful `dotnet build` | `.claude/commands/build.md` |
| `/run` | First successful `dotnet run` | `.claude/commands/run.md` |
| Auto-format Hook | .cs files exceed 5 | `.claude/settings.json` |
| `rules/wpf.md` | WPF patterns explained 3+ times | `.claude/rules/wpf.md` |
| `rules/win32.md` | P/Invoke used 3+ places | `.claude/rules/win32.md` |

## Workflow

```
Explore → Plan → Code → Test → Commit
```

Phases: See `docs/PLAN.md` for details.
