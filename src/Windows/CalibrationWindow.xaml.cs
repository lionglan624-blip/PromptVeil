using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Promptveil.Helpers;

namespace Promptveil.Windows;

/// <summary>
/// Calibration mode window for adjusting overlay position
/// </summary>
public partial class CalibrationWindow : Window
{
    private int _offsetX;
    private int _offsetY;
    private readonly int _initialX;
    private readonly int _initialY;

    public event EventHandler<(int X, int Y)>? CalibrationCompleted;
    public event EventHandler? CalibrationCancelled;

    public int OffsetX => _offsetX;
    public int OffsetY => _offsetY;

    public CalibrationWindow(int initialX, int initialY)
    {
        _initialX = initialX;
        _initialY = initialY;
        _offsetX = initialX;
        _offsetY = initialY;

        InitializeComponent();

        Loaded += CalibrationWindow_Loaded;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.SetWindowExToolWindow(hwnd);
    }

    private void CalibrationWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateDisplay();
        UpdateMarkers();
        Focus();
    }

    public void UpdatePosition(int x, int y, int width, int height)
    {
        Left = x;
        Top = y;
        Width = width;
        Height = height;
        UpdateMarkers();
    }

    private void UpdateDisplay()
    {
        OffsetText.Text = $"オフセット: X={_offsetX}, Y={_offsetY}";
    }

    private void UpdateMarkers()
    {
        // Crosshair at center-bottom (where input overlay will be)
        double centerX = ActualWidth / 2;
        double bottomY = ActualHeight - 50;

        HorizontalLine.X1 = 0;
        HorizontalLine.X2 = ActualWidth;
        HorizontalLine.Y1 = bottomY;
        HorizontalLine.Y2 = bottomY;

        VerticalLine.X1 = centerX;
        VerticalLine.X2 = centerX;
        VerticalLine.Y1 = 0;
        VerticalLine.Y2 = ActualHeight;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        int step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;

        switch (e.Key)
        {
            case Key.Left:
                _offsetX -= step;
                UpdateDisplay();
                e.Handled = true;
                break;

            case Key.Right:
                _offsetX += step;
                UpdateDisplay();
                e.Handled = true;
                break;

            case Key.Up:
                _offsetY -= step;
                UpdateDisplay();
                e.Handled = true;
                break;

            case Key.Down:
                _offsetY += step;
                UpdateDisplay();
                e.Handled = true;
                break;

            case Key.Return:
                CalibrationCompleted?.Invoke(this, (_offsetX, _offsetY));
                Close();
                e.Handled = true;
                break;

            case Key.Escape:
                _offsetX = _initialX;
                _offsetY = _initialY;
                CalibrationCancelled?.Invoke(this, EventArgs.Empty);
                Close();
                e.Handled = true;
                break;
        }
    }
}
