using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Promptveil.Services;

/// <summary>
/// Low-level keyboard hook to detect specific keys (like Right Ctrl)
/// </summary>
public class KeyboardHookService : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int VK_LCONTROL = 0xA2;
    private const int VK_RCONTROL = 0xA3;
    private const int VK_LSHIFT = 0xA0;
    private const int VK_RSHIFT = 0xA1;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _proc;
    private bool _disposed;
    private DateTime _rightCtrlDownTime;
    private DateTime _leftCtrlDownTime;
    private DateTime _shiftDownTime;
    private bool _rightCtrlDown;
    private bool _leftCtrlDown;
    private bool _shiftDown;
    private bool _otherKeyPressed;

    /// <summary>
    /// Fired when Right Ctrl is pressed and released alone (no other keys pressed)
    /// </summary>
    public event EventHandler? RightCtrlPressed;

    /// <summary>
    /// Fired when Left Ctrl is pressed and released alone (no other keys pressed)
    /// </summary>
    public event EventHandler? LeftCtrlPressed;

    /// <summary>
    /// Fired when Shift (either) is pressed and released alone (no other keys pressed)
    /// </summary>
    public event EventHandler? ShiftPressed;

    /// <summary>
    /// Condition function for Right Ctrl - only fire event when this returns true
    /// </summary>
    public Func<bool>? ShouldProcessRightCtrl { get; set; }

    /// <summary>
    /// Condition function for Left Ctrl - only fire event when this returns true
    /// </summary>
    public Func<bool>? ShouldProcessLeftCtrl { get; set; }

    /// <summary>
    /// Condition function for Shift - only fire event when this returns true
    /// </summary>
    public Func<bool>? ShouldProcessShift { get; set; }

    public void Start()
    {
        if (_hookId != IntPtr.Zero)
            return;

        _proc = HookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    public void Stop()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            bool isKeyDown = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
            bool isKeyUp = msg == WM_KEYUP || msg == WM_SYSKEYUP;

            if (hookStruct.vkCode == VK_RCONTROL)
            {
                if (isKeyDown && !_rightCtrlDown)
                {
                    _rightCtrlDown = true;
                    _rightCtrlDownTime = DateTime.Now;
                    _otherKeyPressed = false;
                }
                else if (isKeyUp && _rightCtrlDown)
                {
                    _rightCtrlDown = false;
                    var elapsed = DateTime.Now - _rightCtrlDownTime;

                    if (!_otherKeyPressed && elapsed.TotalMilliseconds < 500)
                    {
                        if (ShouldProcessRightCtrl == null || ShouldProcessRightCtrl())
                        {
                            RightCtrlPressed?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
            else if (hookStruct.vkCode == VK_LCONTROL)
            {
                if (isKeyDown && !_leftCtrlDown)
                {
                    _leftCtrlDown = true;
                    _leftCtrlDownTime = DateTime.Now;
                    _otherKeyPressed = false;
                }
                else if (isKeyUp && _leftCtrlDown)
                {
                    _leftCtrlDown = false;
                    var elapsed = DateTime.Now - _leftCtrlDownTime;

                    if (!_otherKeyPressed && elapsed.TotalMilliseconds < 500)
                    {
                        if (ShouldProcessLeftCtrl == null || ShouldProcessLeftCtrl())
                        {
                            LeftCtrlPressed?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
            else if (hookStruct.vkCode == VK_LSHIFT || hookStruct.vkCode == VK_RSHIFT)
            {
                if (isKeyDown && !_shiftDown)
                {
                    _shiftDown = true;
                    _shiftDownTime = DateTime.Now;
                    _otherKeyPressed = false;
                }
                else if (isKeyUp && _shiftDown)
                {
                    _shiftDown = false;
                    var elapsed = DateTime.Now - _shiftDownTime;

                    if (!_otherKeyPressed && elapsed.TotalMilliseconds < 500)
                    {
                        if (ShouldProcessShift == null || ShouldProcessShift())
                        {
                            ShiftPressed?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
            else if ((_rightCtrlDown || _leftCtrlDown || _shiftDown) && isKeyDown)
            {
                // Another key was pressed while modifier is held
                _otherKeyPressed = true;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();
        _disposed = true;
    }
}
