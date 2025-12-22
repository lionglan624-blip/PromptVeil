Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

public class AllTerminals {
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    public static bool Callback(IntPtr hWnd, IntPtr lParam) {
        StringBuilder className = new StringBuilder(256);
        GetClassName(hWnd, className, 256);
        if (className.ToString() == "CASCADIA_HOSTING_WINDOW_CLASS") {
            StringBuilder title = new StringBuilder(256);
            GetWindowText(hWnd, title, 256);
            RECT rect;
            GetWindowRect(hWnd, out rect);
            bool visible = IsWindowVisible(hWnd);
            Console.WriteLine("Handle={0}, Title={1}, Visible={2}", hWnd, title, visible);
            Console.WriteLine("  Pos: Left={0}, Top={1}, Right={2}, Bottom={3}", rect.Left, rect.Top, rect.Right, rect.Bottom);
        }
        return true;
    }
}
"@

Write-Host "All Windows Terminal windows:"
[AllTerminals]::EnumWindows([AllTerminals+EnumWindowsProc]::CreateDelegate([AllTerminals+EnumWindowsProc], [AllTerminals], "Callback"), [IntPtr]::Zero) | Out-Null
