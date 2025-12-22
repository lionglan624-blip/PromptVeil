using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;
using Promptveil.Helpers;

namespace Promptveil.Services;

/// <summary>
/// Tracks Windows Terminal window position
/// </summary>
public class WindowTracker : IDisposable
{
    private readonly string _targetProcess;
    private readonly string _targetClass;
    private readonly int _pollIntervalMs;

    private IntPtr _targetWindow;
    private IntPtr _eventHook;
    private NativeMethods.WinEventDelegate? _hookDelegate;
    private DispatcherTimer? _pollTimer;
    private NativeMethods.RECT _lastRect;
    private bool _disposed;
    private bool _hasFocus;

    public event EventHandler<NativeMethods.RECT>? WindowMoved;
    public event EventHandler? WindowLost;
    public event EventHandler? FocusLost;
    public event EventHandler? FocusGained;

    public IntPtr TargetWindow => _targetWindow;
    public bool IsTracking => _targetWindow != IntPtr.Zero;

    public WindowTracker(string targetProcess = "WindowsTerminal", string targetClass = "CASCADIA_HOSTING_WINDOW_CLASS", int pollIntervalMs = 50)
    {
        _targetProcess = targetProcess;
        _targetClass = targetClass;
        _pollIntervalMs = pollIntervalMs;
    }

    public bool FindAndTrack()
    {
        _targetWindow = FindTerminalWindow();

        if (_targetWindow == IntPtr.Zero)
            return false;

        StartTracking();
        return true;
    }

    private IntPtr FindTerminalWindow()
    {
        // First, check if foreground window is a terminal
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground != IntPtr.Zero && IsValidTerminalWindow(foreground))
            return foreground;

        // Try finding by class name
        var hwnd = NativeMethods.FindWindow(_targetClass, null);
        if (hwnd != IntPtr.Zero && IsValidTerminalWindow(hwnd))
            return hwnd;

        // Fallback: enumerate windows by process name
        IntPtr found = IntPtr.Zero;
        var processes = Process.GetProcessesByName(_targetProcess);

        foreach (var proc in processes)
        {
            if (proc.MainWindowHandle != IntPtr.Zero)
            {
                found = proc.MainWindowHandle;
                break;
            }
        }

        return found;
    }

    private bool IsValidTerminalWindow(IntPtr hwnd)
    {
        if (!NativeMethods.IsWindow(hwnd) || !NativeMethods.IsWindowVisible(hwnd))
            return false;

        var className = new StringBuilder(256);
        NativeMethods.GetClassName(hwnd, className, className.Capacity);

        return className.ToString() == _targetClass;
    }

    private void StartTracking()
    {
        if (_targetWindow == IntPtr.Zero)
            return;

        // Get initial rect (DPI-aware)
        if (NativeMethods.GetWindowRectDpiAware(_targetWindow, out _lastRect))
        {
            WindowMoved?.Invoke(this, _lastRect);
        }

        // Set initial focus state
        _hasFocus = NativeMethods.GetForegroundWindow() == _targetWindow;

        // Try WinEventHook first
        if (!TrySetupEventHook())
        {
            // Fallback to polling
            SetupPolling();
        }
    }

    private bool TrySetupEventHook()
    {
        try
        {
            NativeMethods.GetWindowThreadProcessId(_targetWindow, out uint processId);

            // Keep delegate alive
            _hookDelegate = new NativeMethods.WinEventDelegate(WinEventProc);

            _eventHook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
                NativeMethods.EVENT_OBJECT_DESTROY,
                IntPtr.Zero,
                _hookDelegate,
                processId,
                0, // All threads in process
                NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);

            return _eventHook != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd != _targetWindow)
            return;

        if (eventType == NativeMethods.EVENT_OBJECT_DESTROY)
        {
            _targetWindow = IntPtr.Zero;
            WindowLost?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (eventType == NativeMethods.EVENT_OBJECT_LOCATIONCHANGE &&
            idObject == NativeMethods.OBJID_WINDOW &&
            idChild == NativeMethods.CHILDID_SELF)
        {
            CheckWindowRect();
        }
    }

    private void SetupPolling()
    {
        _pollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_pollIntervalMs)
        };

        _pollTimer.Tick += (s, e) =>
        {
            if (_targetWindow == IntPtr.Zero)
                return;

            if (!NativeMethods.IsWindow(_targetWindow))
            {
                _targetWindow = IntPtr.Zero;
                WindowLost?.Invoke(this, EventArgs.Empty);
                return;
            }

            CheckWindowRect();
            CheckFocus();
        };

        _pollTimer.Start();
    }

    private void CheckWindowRect()
    {
        if (NativeMethods.GetWindowRectDpiAware(_targetWindow, out var rect))
        {
            if (!RectEquals(rect, _lastRect))
            {
                _lastRect = rect;
                WindowMoved?.Invoke(this, rect);
            }
        }
    }

    private void CheckFocus()
    {
        var foreground = NativeMethods.GetForegroundWindow();

        // Check if a different terminal window became foreground
        if (foreground != _targetWindow && foreground != IntPtr.Zero && IsValidTerminalWindow(foreground))
        {
            // Switch to the new terminal
            SwitchToWindow(foreground);
            return;
        }

        // Check if foreground window belongs to our own process (overlay windows)
        bool isOwnWindow = false;
        if (foreground != IntPtr.Zero)
        {
            NativeMethods.GetWindowThreadProcessId(foreground, out uint foregroundPid);
            uint ourPid = (uint)Process.GetCurrentProcess().Id;
            isOwnWindow = foregroundPid == ourPid;
        }

        // Treat our own windows as "terminal has focus" (don't hide overlay when overlay is focused)
        bool nowHasFocus = foreground == _targetWindow || isOwnWindow;

        if (nowHasFocus != _hasFocus)
        {
            _hasFocus = nowHasFocus;
            if (_hasFocus)
                FocusGained?.Invoke(this, EventArgs.Empty);
            else
                FocusLost?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SwitchToWindow(IntPtr newWindow)
    {
        // Stop tracking old window
        if (_eventHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_eventHook);
            _eventHook = IntPtr.Zero;
        }

        // Switch to new window
        _targetWindow = newWindow;
        _hasFocus = true;

        // Get new window rect and notify
        if (NativeMethods.GetWindowRectDpiAware(_targetWindow, out _lastRect))
        {
            WindowMoved?.Invoke(this, _lastRect);
        }

        // Setup hook for new window
        TrySetupEventHook();

        FocusGained?.Invoke(this, EventArgs.Empty);
    }

    private static bool RectEquals(NativeMethods.RECT r1, NativeMethods.RECT r2)
    {
        return r1.Left == r2.Left && r1.Top == r2.Top &&
               r1.Right == r2.Right && r1.Bottom == r2.Bottom;
    }

    public void StopTracking()
    {
        if (_eventHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_eventHook);
            _eventHook = IntPtr.Zero;
        }

        _pollTimer?.Stop();
        _pollTimer = null;
        _hookDelegate = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        StopTracking();
        _disposed = true;
    }
}
