using System.Drawing;
using System.Runtime.InteropServices;
using Promptveil.Helpers;

namespace Promptveil.Services;

/// <summary>
/// Detects the gray input line separator in Claude Code terminal
/// </summary>
public class InputLineDetector
{
    // Gray line color range for Claude Code separator lines
    // Measured from actual Claude Code: RGB(67,67,67) - brightness ~68
    private const int SeparatorBrightnessMin = 55;
    private const int SeparatorBrightnessMax = 80;
    private const int ConsistencyThreshold = 90; // % of samples that must match

    private static readonly string LogPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Promptveil", "detection.log");

    private static void Log(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n";
            System.IO.File.AppendAllText(LogPath, line);
        }
        catch { }
    }

    /// <summary>
    /// Detect the Y position of the horizontal gray line in the terminal window
    /// Scans from bottom up looking for a horizontal gray line
    /// </summary>
    /// <param name="windowHandle">Terminal window handle</param>
    /// <returns>Y position of the line (screen coordinates), or -1 if not found</returns>
    public int DetectInputLineY(IntPtr windowHandle)
    {
        if (!NativeMethods.GetWindowRectDpiAware(windowHandle, out var rect))
            return -1;

        int width = rect.Width;
        int height = rect.Height;

        if (width <= 0 || height <= 0)
            return -1;

        // Capture window content
        using var bitmap = CaptureWindow(windowHandle, rect);
        if (bitmap == null)
            return -1;

        // Scan from bottom up, looking for horizontal gray line
        // Start from ~20% from bottom, end at ~80% from bottom
        int startY = (int)(height * 0.8);
        int endY = (int)(height * 0.2);

        for (int y = startY; y >= endY; y--)
        {
            if (IsHorizontalGrayLine(bitmap, y, width))
            {
                // Found a line, return screen Y coordinate
                return rect.Top + y;
            }
        }

        return -1;
    }

    /// <summary>
    /// Detect both top and bottom gray lines (input area bounds)
    /// </summary>
    public (int topY, int bottomY) DetectInputAreaBounds(IntPtr windowHandle)
    {
        if (!NativeMethods.GetWindowRectDpiAware(windowHandle, out var rect))
            return (-1, -1);

        int width = rect.Width;
        int height = rect.Height;

        if (width <= 0 || height <= 0)
            return (-1, -1);

        using var bitmap = CaptureWindow(windowHandle, rect);
        if (bitmap == null)
            return (-1, -1);

        // Debug: Save captured image
        try
        {
            string debugPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Promptveil", "debug_capture.png");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(debugPath)!);
            bitmap.Save(debugPath, System.Drawing.Imaging.ImageFormat.Png);
            System.Diagnostics.Debug.WriteLine($"Saved debug capture to: {debugPath}");
        }
        catch { }

        int topLine = -1;
        int bottomLine = -1;
        int bottomLineY = -1; // bitmap Y coordinate of bottom line
        const int minLineSeparation = 20; // Minimum pixels between distinct lines
        const int maxInputAreaHeight = 200; // Maximum expected height of input area in pixels

        // Scan from bottom up to find the FIRST gray line (bottom of input area)
        int startY = (int)(height * 0.95);
        int endY = (int)(height * 0.2);

        Log($"Scanning from Y={startY} to Y={endY} (height={height}), windowRect=({rect.Left},{rect.Top},{rect.Right},{rect.Bottom})");

        for (int y = startY; y >= endY; y--)
        {
            if (IsHorizontalGrayLine(bitmap, y, width))
            {
                Log($"Found gray line at bitmap Y={y} -> screen Y={rect.Top + y}");
                bottomLine = rect.Top + y;
                bottomLineY = y;
                Log($"  -> Set as BOTTOM line: {bottomLine}");
                break; // Found bottom line
            }
        }

        // If bottom line found, search for top line within limited range
        if (bottomLine != -1 && bottomLineY > 0)
        {
            // Search from just above bottom line, within maxInputAreaHeight
            int topSearchStart = bottomLineY - minLineSeparation;
            int topSearchEnd = Math.Max(0, bottomLineY - maxInputAreaHeight);

            Log($"Searching for TOP line from Y={topSearchStart} to Y={topSearchEnd}");

            for (int y = topSearchStart; y >= topSearchEnd; y--)
            {
                if (IsHorizontalGrayLine(bitmap, y, width))
                {
                    Log($"Found gray line at bitmap Y={y} -> screen Y={rect.Top + y}");
                    topLine = rect.Top + y;
                    Log($"  -> Set as TOP line: {topLine}");
                    break; // Found top line
                }
            }
        }

        Log($"Detection result: top={topLine}, bottom={bottomLine}");
        return (topLine, bottomLine);
    }

    private Bitmap? CaptureWindow(IntPtr hwnd, NativeMethods.RECT rect)
    {
        try
        {
            int width = rect.Width;
            int height = rect.Height;

            System.Diagnostics.Debug.WriteLine($"CaptureWindow: rect=({rect.Left},{rect.Top},{rect.Right},{rect.Bottom}), size={width}x{height}");

            // Use screen capture instead of PrintWindow for better compatibility
            var hdcScreen = GetDC(IntPtr.Zero);
            var hdcDest = CreateCompatibleDC(hdcScreen);
            var hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
            var hOld = SelectObject(hdcDest, hBitmap);

            // BitBlt from screen at window position
            BitBlt(hdcDest, 0, 0, width, height, hdcScreen, rect.Left, rect.Top, SRCCOPY);

            SelectObject(hdcDest, hOld);
            DeleteDC(hdcDest);
            ReleaseDC(IntPtr.Zero, hdcScreen);

            var bitmap = Image.FromHbitmap(hBitmap);
            DeleteObject(hBitmap);

            System.Diagnostics.Debug.WriteLine($"CaptureWindow: bitmap size = {bitmap.Width}x{bitmap.Height}");

            return bitmap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CaptureWindow failed: {ex.Message}");
            return null;
        }
    }

    private bool IsHorizontalGrayLine(Bitmap bitmap, int y, int width)
    {
        if (y < 0 || y >= bitmap.Height)
            return false;

        // Sample brightness values across the row (skip edges)
        int margin = Math.Min(50, width / 20);
        int sampleStep = Math.Max(1, width / 100);
        var brightnessValues = new List<int>();

        for (int x = margin; x < width - margin; x += sampleStep)
        {
            if (x >= bitmap.Width) break;
            var pixel = bitmap.GetPixel(x, y);
            int brightness = (pixel.R + pixel.G + pixel.B) / 3;
            brightnessValues.Add(brightness);
        }

        if (brightnessValues.Count == 0)
            return false;

        // Calculate average brightness
        int avgBrightness = brightnessValues.Sum() / brightnessValues.Count;

        // Check if brightness is in separator line range
        if (avgBrightness < SeparatorBrightnessMin || avgBrightness > SeparatorBrightnessMax)
            return false;

        // Check consistency - most samples should be similar
        int consistentCount = brightnessValues.Count(b => Math.Abs(b - avgBrightness) < 15);
        int consistencyPct = consistentCount * 100 / brightnessValues.Count;

        bool isLine = consistencyPct >= ConsistencyThreshold;

        if (isLine || (avgBrightness >= SeparatorBrightnessMin && avgBrightness <= SeparatorBrightnessMax))
        {
            Log($"  Y={y}: brightness={avgBrightness}, consistency={consistencyPct}%, isLine={isLine}");
        }

        return isLine;
    }

    #region P/Invoke

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
        IntPtr hdcSrc, int xSrc, int ySrc, uint rop);

    private const uint PW_RENDERFULLCONTENT = 0x00000002;
    private const uint SRCCOPY = 0x00CC0020;

    #endregion
}
