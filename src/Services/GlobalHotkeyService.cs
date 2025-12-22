using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Promptveil.Helpers;

namespace Promptveil.Services;

/// <summary>
/// Manages global hotkey registration
/// </summary>
public class GlobalHotkeyService : IDisposable
{
    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private IntPtr _hwnd;
    private HwndSource? _source;
    private int _nextId = 1;
    private bool _disposed;

    public void Initialize(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _hwnd = helper.EnsureHandle();

        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WndProc);
    }

    public int RegisterHotkey(NativeMethods.KeyModifiers modifiers, Key key, Action callback)
    {
        if (_hwnd == IntPtr.Zero)
            throw new InvalidOperationException("Service not initialized");

        int id = _nextId++;
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

        if (!NativeMethods.RegisterHotKey(_hwnd, id, (uint)modifiers, vk))
        {
            throw new InvalidOperationException($"ホットキーの登録に失敗しました: {modifiers}+{key}");
        }

        _hotkeyActions[id] = callback;
        return id;
    }

    public void UnregisterHotkey(int id)
    {
        if (_hwnd != IntPtr.Zero && _hotkeyActions.ContainsKey(id))
        {
            NativeMethods.UnregisterHotKey(_hwnd, id);
            _hotkeyActions.Remove(id);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                action.Invoke();
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var id in _hotkeyActions.Keys.ToList())
        {
            UnregisterHotkey(id);
        }

        _source?.RemoveHook(WndProc);
        _source = null;
        _disposed = true;
    }
}
