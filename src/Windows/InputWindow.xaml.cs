using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Promptveil.Helpers;
using Promptveil.Services;

namespace Promptveil.Windows;

/// <summary>
/// Overlay input window with TextBox for IME input
/// </summary>
public partial class InputWindow : Window
{
    private readonly ClipboardInjector _injector;
    private readonly ConfigService _configService;
    private readonly List<string> _history;
    private int _historyIndex = -1;
    private string _currentInput = "";

    // Right-click drag state
    private bool _isDragging = false;
    private Point _dragStartPoint;
    private double _windowStartLeft;
    private double _windowStartTop;

    // Ctrl key tracking for solo Ctrl redetect
    private bool _ctrlUsedWithOtherKey = false;

    private static readonly string LogPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Promptveil", "detection.log");

    private static void Log(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] [InputWindow] {message}\n";
            System.IO.File.AppendAllText(LogPath, line);
        }
        catch { }
    }

    public event EventHandler? SendRequested;
    public event EventHandler? HideRequested;
    public event EventHandler<(double deltaX, double deltaY)>? PositionChanged;
    public event EventHandler? RedetectRequested;

    public new double FontSize { get; set; } = 14;
    public IntPtr TargetWindow { get; set; }

    public string InputText
    {
        get => InputBox.Text;
        set => InputBox.Text = value;
    }

    public InputWindow(ClipboardInjector injector, ConfigService configService)
    {
        _injector = injector;
        _configService = configService;
        _history = configService.Config.History;

        DataContext = this;
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;

        // Hide from Alt+Tab
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

    public void FocusInput()
    {
        InputBox.Focus();
        Keyboard.Focus(InputBox);
    }

    public void ClearInput()
    {
        InputBox.Clear();
        _historyIndex = -1;
        _currentInput = "";
    }

    private async void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            e.Handled = true;
            await SendInputAsync();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            ClearInput();
        }
    }

    private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        Log($"PreviewKeyDown: Key={e.Key}, Modifiers={Keyboard.Modifiers}");

        // History navigation with Ctrl+Up/Down
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            if (e.Key == Key.Up)
            {
                e.Handled = true;
                NavigateHistory(-1);
            }
            else if (e.Key == Key.Down)
            {
                e.Handled = true;
                NavigateHistory(1);
            }
        }

        // Ctrl+L to clear
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.L)
        {
            Log($"Ctrl+L pressed - clearing input");
            e.Handled = true;
            _ctrlUsedWithOtherKey = true;
            ClearInput();
        }

        // Track if Ctrl was pressed alone (for redetect on release)
        if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
        {
            _ctrlUsedWithOtherKey = false;
        }
        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            _ctrlUsedWithOtherKey = true;
        }
    }

    private void InputBox_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        // Ctrl released alone -> redetect
        if ((e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) && !_ctrlUsedWithOtherKey)
        {
            Log($"Ctrl released alone - requesting redetect");
            e.Handled = true;
            RedetectRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void NavigateHistory(int direction)
    {
        if (_history.Count == 0)
            return;

        // Save current input when starting navigation
        if (_historyIndex == -1)
        {
            _currentInput = InputBox.Text;
        }

        int newIndex = _historyIndex + direction;

        // Going backwards (older)
        if (direction < 0)
        {
            if (_historyIndex == -1)
            {
                // Start from end of history
                newIndex = _history.Count - 1;
            }
            else if (newIndex < 0)
            {
                // Stay at oldest
                return;
            }
        }
        // Going forwards (newer)
        else
        {
            if (newIndex >= _history.Count)
            {
                // Return to current input
                _historyIndex = -1;
                InputBox.Text = _currentInput;
                InputBox.CaretIndex = InputBox.Text.Length;
                return;
            }
        }

        _historyIndex = newIndex;
        InputBox.Text = _history[_historyIndex];
        InputBox.CaretIndex = InputBox.Text.Length;
    }

    private async Task SendInputAsync()
    {
        var text = InputBox.Text;

        if (string.IsNullOrEmpty(text) && !_configService.Config.SendEmptyEnter)
            return;

        if (TargetWindow == IntPtr.Zero)
            return;

        // Add to history
        if (!string.IsNullOrWhiteSpace(text))
        {
            _configService.AddHistory(text);
        }

        // Clear input
        ClearInput();

        // Inject text
        await _injector.InjectTextAsync(TargetWindow, text, sendEnter: true);

        // Return focus to overlay
        await Task.Delay(100);
        FocusInput();

        SendRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CloseButtonArea_MouseEnter(object sender, MouseEventArgs e)
    {
        CloseButton.Visibility = Visibility.Visible;
    }

    private void CloseButtonArea_MouseLeave(object sender, MouseEventArgs e)
    {
        CloseButton.Visibility = Visibility.Hidden;
    }

    private void CloseButton_Click(object sender, MouseButtonEventArgs e)
    {
        HideRequested?.Invoke(this, EventArgs.Empty);
    }

    #region Right-click drag to move

    private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        Log($"Right mouse button DOWN at {e.GetPosition(this)}");
        _isDragging = true;
        _dragStartPoint = e.GetPosition(this);
        _windowStartLeft = Left;
        _windowStartTop = Top;
        CaptureMouse();
        e.Handled = true;
    }

    private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        Log($"Right mouse button UP, isDragging={_isDragging}");
        if (_isDragging)
        {
            _isDragging = false;
            ReleaseMouseCapture();

            // Notify parent about position change (in logical pixels)
            var deltaX = Left - _windowStartLeft;
            var deltaY = Top - _windowStartTop;
            Log($"Position change: deltaX={deltaX:F1}, deltaY={deltaY:F1}");
            if (Math.Abs(deltaX) > 1 || Math.Abs(deltaY) > 1)
            {
                PositionChanged?.Invoke(this, (deltaX, deltaY));
            }
        }
        e.Handled = true;
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var currentPoint = e.GetPosition(this);
            var deltaX = currentPoint.X - _dragStartPoint.X;
            var deltaY = currentPoint.Y - _dragStartPoint.Y;

            Left += deltaX;
            Top += deltaY;
        }
    }

    #endregion
}
