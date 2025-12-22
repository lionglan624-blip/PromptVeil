using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Promptveil.Helpers;
using Promptveil.Services;
using Promptveil.Windows;

namespace Promptveil;

/// <summary>
/// Main application - tray-resident overlay manager
/// </summary>
public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MaskWindow? _maskWindow;
    private InputWindow? _inputWindow;
    private CalibrationWindow? _calibrationWindow;

    private ConfigService _configService = null!;
    private WindowTracker _windowTracker = null!;
    private ClipboardInjector _clipboardInjector = null!;
    private GlobalHotkeyService _hotkeyService = null!;
    private InputLineDetector _lineDetector = null!;

    private bool _overlayVisible = true;
    private bool _paused = false;
    private bool _isCalibrating = false;
    private (int topY, int bottomY) _detectedInputArea = (-1, -1);
    private NativeMethods.RECT _lastTrackedRect;
    private bool _pendingLineDetection = false;
    private DispatcherTimer? _moveEndTimer;

    private static readonly string LogPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Promptveil", "detection.log");

    private static void Log(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] [App] {message}\n";
            System.IO.File.AppendAllText(LogPath, line);
        }
        catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Ensure Per-Monitor DPI V2 awareness
        NativeMethods.SetProcessDpiAwarenessContext(NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

        // Initialize services
        _configService = new ConfigService();
        _configService.Load();

        _clipboardInjector = new ClipboardInjector(_configService.Config.PasteDelayMs);
        _windowTracker = new WindowTracker(
            _configService.Config.TargetProcess,
            _configService.Config.TerminalClass,
            _configService.Config.PollIntervalMs);
        _lineDetector = new InputLineDetector();

        // Create windows
        CreateWindows();

        // Create tray icon
        CreateTrayIcon();

        // Setup hotkey service (needs a window handle)
        SetupHotkeys();

        // Setup window tracker
        SetupWindowTracker();

        // Start tracking
        TryStartTracking();
    }

    private void CreateWindows()
    {
        _maskWindow = new MaskWindow();
        _inputWindow = new InputWindow(_clipboardInjector, _configService)
        {
            FontSize = _configService.Config.FontSize
        };

        _inputWindow.HideRequested += (s, e) => HideOverlay();
        _inputWindow.SendRequested += (s, e) => TriggerLineDetection();
        _inputWindow.PositionChanged += OnInputWindowPositionChanged;
        _inputWindow.RedetectRequested += (s, e) => ForceRedetect();
    }

    private void OnInputWindowPositionChanged(object? sender, (double deltaX, double deltaY) delta)
    {
        // Move mask window together with input window
        if (_maskWindow != null)
        {
            _maskWindow.Left += delta.deltaX;
            _maskWindow.Top += delta.deltaY;
        }

        // Convert logical pixels to physical for storage
        var source = System.Windows.PresentationSource.FromVisual(_inputWindow!);
        double dpiScale = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        int deltaYPhysical = (int)(delta.deltaY * dpiScale);

        // If we have detected area, update it directly (keeps detection mode active)
        if (_detectedInputArea.topY > 0 && _detectedInputArea.bottomY > 0)
        {
            _detectedInputArea = (_detectedInputArea.topY + deltaYPhysical, _detectedInputArea.bottomY + deltaYPhysical);
            Log($"Position manually adjusted: deltaY={delta.deltaY:F1}, updated detectedArea to ({_detectedInputArea.topY}, {_detectedInputArea.bottomY})");
        }
        else
        {
            // Fallback mode: update config offset
            _configService.Config.OffsetY += deltaYPhysical;
            _configService.Save();
            Log($"Position manually adjusted: deltaY={delta.deltaY:F1}, new OffsetY={_configService.Config.OffsetY}");
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    private void CreateTrayIcon()
    {
        try
        {
            var contextMenu = (ContextMenu)FindResource("TrayMenu");

            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "Terminal Input Overlay",
                ContextMenu = contextMenu,
                MenuActivation = PopupActivationMode.RightClick
            };

            // Create icon using System.Drawing and set directly to Icon property
            _trayIcon.Icon = CreateSystemDrawingIcon();

            // Force create the tray icon
            _trayIcon.ForceCreate();

            _trayIcon.TrayLeftMouseDown += TrayIcon_LeftClick;
        }
        catch
        {
            // Tray icon creation failed - app will still work but without tray icon
        }
    }

    private System.Drawing.Icon CreateSystemDrawingIcon()
    {
        // Create a 16x16 icon with ">" symbol
        using var bitmap = new System.Drawing.Bitmap(16, 16);
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);

        graphics.Clear(System.Drawing.Color.FromArgb(30, 30, 30));
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 200, 100), 2);
        graphics.DrawLine(pen, 4, 3, 11, 8);
        graphics.DrawLine(pen, 11, 8, 4, 13);

        // Convert to icon by saving to stream
        IntPtr hIcon = bitmap.GetHicon();
        var tempIcon = System.Drawing.Icon.FromHandle(hIcon);

        // Save to stream and reload to own the icon data
        using var ms = new System.IO.MemoryStream();
        tempIcon.Save(ms);
        ms.Position = 0;
        var result = new System.Drawing.Icon(ms);

        DestroyIcon(hIcon);
        return result;
    }

    private void SetupHotkeys()
    {
        _hotkeyService = new GlobalHotkeyService();

        // Need a hidden window for hotkey registration
        var hiddenWindow = new Window
        {
            Width = 0,
            Height = 0,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false
        };
        hiddenWindow.Show();
        hiddenWindow.Hide();

        _hotkeyService.Initialize(hiddenWindow);

        // Register Ctrl+Alt+C for calibration
        try
        {
            _hotkeyService.RegisterHotkey(
                NativeMethods.KeyModifiers.Control | NativeMethods.KeyModifiers.Alt,
                Key.C,
                () => Dispatcher.Invoke(StartCalibration));
        }
        catch
        {
            // Hotkey may already be registered by another app
        }

    }

    private void SetupWindowTracker()
    {
        // Timer to detect end of window movement
        _moveEndTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _moveEndTimer.Tick += (s, e) =>
        {
            _moveEndTimer.Stop();
            Log("Window move ended - triggering re-detection");
            TriggerLineDetection();
        };

        _windowTracker.WindowMoved += (s, rect) =>
        {
            if (_paused || _isCalibrating)
                return;

            Dispatcher.Invoke(() =>
            {
                // Check if window size changed (triggers line detection)
                bool sizeChanged = _lastTrackedRect.Width != rect.Width || _lastTrackedRect.Height != rect.Height;
                bool positionChanged = _lastTrackedRect.Left != rect.Left || _lastTrackedRect.Top != rect.Top;

                // Update detected area coordinates when window moves (not resizes)
                if (!sizeChanged && _detectedInputArea.topY > 0 && _detectedInputArea.bottomY > 0 && _lastTrackedRect.Width > 0)
                {
                    int deltaY = rect.Top - _lastTrackedRect.Top;
                    if (deltaY != 0)
                    {
                        _detectedInputArea = (_detectedInputArea.topY + deltaY, _detectedInputArea.bottomY + deltaY);
                        Log($"Window moved: deltaY={deltaY}, updated detectedArea to ({_detectedInputArea.topY}, {_detectedInputArea.bottomY})");
                    }
                }

                _lastTrackedRect = rect;

                if (sizeChanged && _overlayVisible)
                {
                    TriggerLineDetection();
                }
                else if (positionChanged && _overlayVisible)
                {
                    // Reset timer on each move event - will fire 300ms after last move
                    _moveEndTimer.Stop();
                    _moveEndTimer.Start();
                }

                UpdateOverlayPosition(rect);
            });
        };

        _windowTracker.WindowLost += (s, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                HideOverlay();
                // Try to find terminal again after delay
                _ = RetryTrackingAsync();
            });
        };

        _windowTracker.FocusLost += (s, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (_overlayVisible && !_paused)
                {
                    _maskWindow?.Hide();
                    _inputWindow?.Hide();
                }
            });
        };

        _windowTracker.FocusGained += (s, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (_overlayVisible && !_paused)
                {
                    _maskWindow?.Show();
                    _inputWindow?.Show();
                    _inputWindow?.FocusInput();
                }
            });
        };
    }

    private async Task RetryTrackingAsync()
    {
        await Task.Delay(1000);

        if (!_windowTracker.IsTracking)
        {
            TryStartTracking();
        }
    }

    private void TryStartTracking()
    {
        Log($"TryStartTracking: FindAndTrack...");
        if (_windowTracker.FindAndTrack())
        {
            Log($"TryStartTracking: Found window, overlayVisible={_overlayVisible}, paused={_paused}");
            if (_overlayVisible && !_paused)
            {
                ShowOverlay();
            }
        }
        else
        {
            Log("TryStartTracking: No window found");
        }
    }

    private void UpdateOverlayPosition(NativeMethods.RECT terminalRect)
    {
        var config = _configService.Config;

        // Get DPI scale for the target window (for scaling config values)
        double dpiScale = NativeMethods.GetDpiScaleForWindow(_windowTracker.TargetWindow);

        int x = terminalRect.Left + (int)(config.OffsetX * dpiScale);
        int width = terminalRect.Width - (int)(config.OffsetX * 2 * dpiScale);

        int maskY, maskHeight, inputY, inputHeight;
        string positionSource;

        // Use detected input area if available
        if (_detectedInputArea.topY > 0 && _detectedInputArea.bottomY > 0)
        {
            // Use detected gray lines (already in physical pixels)
            maskY = _detectedInputArea.topY;
            maskHeight = _detectedInputArea.bottomY - _detectedInputArea.topY;
            inputHeight = (int)(config.InputHeightPx * dpiScale);
            inputY = maskY + (maskHeight - inputHeight) / 2;
            positionSource = "DETECTED";
        }
        else
        {
            // Fallback to config-based positioning (scale config values to physical pixels)
            maskHeight = (int)(config.MaskLines * config.LineHeightPx * dpiScale);
            inputHeight = (int)(config.InputHeightPx * dpiScale);
            maskY = terminalRect.Bottom + (int)(config.OffsetY * dpiScale) - maskHeight;
            inputY = maskY + (maskHeight - inputHeight) / 2;
            positionSource = "FALLBACK";
        }

        Log($"UpdatePosition: Source={positionSource}, MaskY={maskY}, MaskH={maskHeight}, DpiScale={dpiScale:F2}, DetectedArea=({_detectedInputArea.topY},{_detectedInputArea.bottomY}), terminalRect=({terminalRect.Left},{terminalRect.Top},{terminalRect.Right},{terminalRect.Bottom})");

        // Update mask position
        _maskWindow?.UpdatePosition(x, maskY, width, maskHeight);

        // Update input position (centered in mask area)
        _inputWindow?.UpdatePosition(x, inputY, width, inputHeight);

        // Update target window for input injection
        if (_inputWindow != null)
        {
            _inputWindow.TargetWindow = _windowTracker.TargetWindow;
        }
    }

    private void TriggerLineDetection()
    {
        if (_pendingLineDetection || !_windowTracker.IsTracking)
            return;

        _pendingLineDetection = true;
        Log("TriggerLineDetection started");

        // Hide overlay temporarily before capture to avoid capturing ourselves
        Dispatcher.Invoke(() =>
        {
            _maskWindow?.Hide();
            _inputWindow?.Hide();
        });

        // Delay detection to allow terminal to update after Enter
        _ = Task.Run(async () =>
        {
            await Task.Delay(500); // Wait for overlay to hide and window to settle

            try
            {
                // Re-get window rect right before capture (window may have moved)
                if (!NativeMethods.GetWindowRectDpiAware(_windowTracker.TargetWindow, out var currentRect))
                {
                    Log("Failed to get window rect before capture");
                    return;
                }
                Log($"Capture window rect: ({currentRect.Left},{currentRect.Top},{currentRect.Right},{currentRect.Bottom})");

                var bounds = _lineDetector.DetectInputAreaBounds(_windowTracker.TargetWindow);

                Log($"Detection returned: top={bounds.topY}, bottom={bounds.bottomY}");

                await Dispatcher.InvokeAsync(() =>
                {
                    if (bounds.topY > 0 && bounds.bottomY > 0)
                    {
                        _detectedInputArea = bounds;
                        Log($"Updated _detectedInputArea to: ({bounds.topY}, {bounds.bottomY})");

                        // Refresh overlay position with new detected area
                        if (_windowTracker.IsTracking && NativeMethods.GetWindowRectDpiAware(_windowTracker.TargetWindow, out var rect))
                        {
                            UpdateOverlayPosition(rect);
                        }
                    }
                    else
                    {
                        Log("Detection failed - keeping previous position");
                    }

                    // Show overlay again
                    if (_overlayVisible && !_paused)
                    {
                        _maskWindow?.Show();
                        _inputWindow?.Show();
                        _inputWindow?.FocusInput();
                    }
                });
            }
            finally
            {
                _pendingLineDetection = false;
            }
        });
    }

    private void ForceRedetect()
    {
        if (!_windowTracker.IsTracking)
            return;

        Log("ForceRedetect triggered by hotkey (Ctrl+Alt+R)");

        // Clear detected area to force re-detection
        _detectedInputArea = (-1, -1);

        // Trigger detection
        TriggerLineDetection();
    }

    private void ShowOverlay()
    {
        _overlayVisible = true;
        Log($"ShowOverlay called, maskWindow={_maskWindow != null}, inputWindow={_inputWindow != null}");

        _maskWindow?.Show();
        _inputWindow?.Show();
        _inputWindow?.FocusInput();

        Log($"After Show: maskVisible={_maskWindow?.IsVisible}, inputVisible={_inputWindow?.IsVisible}");

        // Temporarily disable line detection to debug overlay display
        // TriggerLineDetection();

        UpdateMenuState();
    }

    private void HideOverlay()
    {
        _overlayVisible = false;
        _maskWindow?.Hide();
        _inputWindow?.Hide();

        UpdateMenuState();
    }

    private void ToggleOverlay()
    {
        if (_overlayVisible)
        {
            HideOverlay();
        }
        else
        {
            // Re-find terminal window (may have changed or now be foreground)
            _windowTracker.StopTracking();
            if (_windowTracker.FindAndTrack())
            {
                ShowOverlay();
            }
        }
    }

    private void UpdateMenuState()
    {
        var menu = _trayIcon?.ContextMenu;
        if (menu == null)
            return;

        var toggleItem = menu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "ToggleMenuItem");
        var pauseItem = menu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "PauseMenuItem");

        if (toggleItem != null)
        {
            toggleItem.Header = _overlayVisible ? "オーバーレイを非表示" : "オーバーレイを表示";
        }

        if (pauseItem != null)
        {
            pauseItem.Header = _paused ? "再開" : "一時停止";
        }
    }

    private void StartCalibration()
    {
        if (_isCalibrating || !_windowTracker.IsTracking)
            return;

        _isCalibrating = true;
        HideOverlay();

        var config = _configService.Config;

        if (NativeMethods.GetWindowRect(_windowTracker.TargetWindow, out var rect))
        {
            _calibrationWindow = new CalibrationWindow(config.OffsetX, config.OffsetY);
            _calibrationWindow.UpdatePosition(rect.Left, rect.Top, rect.Width, rect.Height);

            _calibrationWindow.CalibrationCompleted += (s, offsets) =>
            {
                config.OffsetX = offsets.X;
                config.OffsetY = offsets.Y;
                _configService.Save();

                _isCalibrating = false;
                _calibrationWindow = null;

                if (_overlayVisible)
                    ShowOverlay();
            };

            _calibrationWindow.CalibrationCancelled += (s, e) =>
            {
                _isCalibrating = false;
                _calibrationWindow = null;

                if (_overlayVisible)
                    ShowOverlay();
            };

            _calibrationWindow.Show();
            _calibrationWindow.Focus();
        }
        else
        {
            _isCalibrating = false;
        }
    }

    #region Tray Menu Event Handlers

    private void TrayIcon_LeftClick(object sender, RoutedEventArgs e)
    {
        ToggleOverlay();
    }

    private void ToggleOverlay_Click(object sender, RoutedEventArgs e)
    {
        ToggleOverlay();
    }

    private void Calibration_Click(object sender, RoutedEventArgs e)
    {
        StartCalibration();
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        _paused = !_paused;

        if (_paused)
        {
            HideOverlay();
        }
        else if (_windowTracker.IsTracking)
        {
            ShowOverlay();
        }

        UpdateMenuState();
    }

    private void ReloadConfig_Click(object sender, RoutedEventArgs e)
    {
        _configService.Load();

        // Update services with new config
        if (_inputWindow != null)
        {
            _inputWindow.FontSize = _configService.Config.FontSize;
        }

        // Refresh position
        if (_windowTracker.IsTracking && NativeMethods.GetWindowRect(_windowTracker.TargetWindow, out var rect))
        {
            UpdateOverlayPosition(rect);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Shutdown();
    }

    #endregion

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Dispose();
        _windowTracker?.Dispose();
        _trayIcon?.Dispose();

        _maskWindow?.Close();
        _inputWindow?.Close();
        _calibrationWindow?.Close();

        base.OnExit(e);
    }
}
