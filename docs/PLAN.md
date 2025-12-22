# Project Plan: Terminal Input Overlay

## Phase Overview

| Phase | Status | Description |
|-------|--------|-------------|
| 0 | ✅ Complete | Technical validation planning |
| 1 | ✅ Complete | Technical spike (click-through + IME POC) |
| 2 | ✅ Complete | MVP (tray + overlay + input sending) |
| 3 | ✅ Complete | Window tracking + calibration |
| 4 | ✅ Complete | Polish (history, hover x, high DPI) |

---

## Phase 0: Technical Validation Planning

**Goal**: Identify technical risks and validation items before implementation.

### Validation Items

| Item | Risk Level | Resolution |
|------|------------|------------|
| Click-through + IME | High | Solved: Separate MaskWindow (click-through) and InputWindow (normal) |
| WS_EX_TRANSPARENT | Medium | Implemented with 64-bit compatible GetWindowLongPtr/SetWindowLongPtr |
| High DPI | Medium | Per-monitor DPI V2 via app.manifest + DPI virtualization correction |
| Clipboard injection | Low | Backup/restore pattern with configurable delay |

### Exit Criteria
- [x] All validation items documented
- [x] Risk mitigation strategies identified
- [x] POC scope defined

---

## Phase 1: Technical Spike

**Goal**: Validate the riskiest technical assumptions with minimal code.

### POC Scope
- [x] Create minimal WPF window
- [x] Implement click-through behavior
- [x] Add TextBox with IME support
- [x] Verify IME candidates display correctly
- [x] Test on high DPI (150%)

### Exit Criteria
- [x] Click-through works
- [x] IME input works correctly
- [x] No critical issues found

---

## Phase 2: MVP (Minimum Viable Product)

**Goal**: Working application with core functionality.

### Features
- [x] Tray icon with context menu
- [x] Overlay window (mask + input field)
- [x] Input sending via clipboard
- [x] Basic keyboard shortcuts (Enter, Esc)

### Exit Criteria
- [x] Can type Japanese in overlay
- [x] Text sent to terminal correctly
- [x] Application runs from tray

---

## Phase 3: Window Tracking + Calibration

**Goal**: Overlay follows terminal window.

### Features
- [x] Terminal window detection
- [x] Position tracking (WinEventHook with polling fallback)
- [x] Calibration mode (Ctrl+Alt+C)
- [x] Config persistence (config.json)

### Exit Criteria
- [x] Overlay follows terminal movement
- [x] Calibration offsets saved/loaded
- [x] Works after terminal resize

---

## Phase 4: Polish

**Goal**: Production-ready quality.

### Features
- [x] Command history (Ctrl+Up/Down)
- [x] Hover x button
- [x] High DPI support
- [x] Error handling
- [x] Tray menu completion

### Exit Criteria
- [x] All acceptance criteria from readme.txt met
- [x] No known critical bugs
- [x] Ready for daily use

---

## Implementation Summary

### Files Created

```
Promptveil/
├── Promptveil.csproj           # .NET 8 WPF project
├── app.manifest                # Per-Monitor DPI V2
├── src/
│   ├── App.xaml                # Application with tray menu
│   ├── App.xaml.cs             # Main application logic
│   ├── Windows/
│   │   ├── MaskWindow.xaml     # Click-through black mask
│   │   ├── MaskWindow.xaml.cs
│   │   ├── InputWindow.xaml    # Overlay input TextBox
│   │   ├── InputWindow.xaml.cs
│   │   ├── CalibrationWindow.xaml    # Calibration mode UI
│   │   └── CalibrationWindow.xaml.cs
│   ├── Services/
│   │   ├── ConfigService.cs          # JSON config persistence
│   │   ├── ClipboardInjector.cs      # Clipboard paste injection
│   │   ├── WindowTracker.cs          # Terminal window tracking
│   │   └── GlobalHotkeyService.cs    # Ctrl+Alt+C hotkey
│   ├── Models/
│   │   └── Config.cs                 # Configuration model
│   └── Helpers/
│       └── NativeMethods.cs          # Win32 P/Invoke
└── docs/
    └── PLAN.md                       # This file
```

### Key Technical Decisions

1. **Two-Window Architecture**: MaskWindow (click-through) + InputWindow (normal)
2. **64-bit Compatible P/Invoke**: GetWindowLongPtr/SetWindowLongPtr
3. **WinEventHook with Fallback**: Primary event-driven, fallback to polling
4. **H.NotifyIcon.Wpf**: Pure WPF tray implementation
5. **DPI Virtualization Handling**: GetWindowRectDpiAware corrects virtualized coordinates on high-DPI displays
6. **Foreground Window Priority**: Tracks foreground terminal when toggling overlay

### Usage

1. Run `dotnet run` or `dotnet build` then execute `bin/Debug/net8.0-windows/Promptveil.exe`
2. Application starts in system tray
3. Left-click tray icon to toggle overlay
4. Press Ctrl+Alt+C to enter calibration mode
5. Right-click tray icon for menu options

---

## Status Legend

- 🔲 Pending
- 🔄 In Progress
- ✅ Complete
- ❌ Blocked
