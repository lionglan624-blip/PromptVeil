using System.Windows;
using System.Windows.Interop;
using Promptveil.Helpers;

namespace Promptveil.Windows;

/// <summary>
/// Click-through black mask window that covers terminal input area
/// </summary>
public partial class MaskWindow : Window
{
    public MaskWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;

        // Make click-through and hide from Alt+Tab
        NativeMethods.SetWindowExTransparent(hwnd);
        NativeMethods.SetWindowExToolWindow(hwnd);
    }

    public void UpdatePosition(int x, int y, int width, int height)
    {
        // WPF uses device-independent pixels (96 DPI base)
        // Physical pixel coordinates need to be converted
        var source = PresentationSource.FromVisual(this);
        double dpiScale = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

        Left = x / dpiScale;
        Top = y / dpiScale;
        Width = width / dpiScale;
        Height = height / dpiScale;
    }
}
