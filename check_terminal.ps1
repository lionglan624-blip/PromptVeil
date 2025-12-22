Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class TerminalPos {
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    public static void Check() {
        IntPtr hwnd = FindWindow("CASCADIA_HOSTING_WINDOW_CLASS", null);
        if (hwnd != IntPtr.Zero) {
            RECT rect;
            GetWindowRect(hwnd, out rect);
            Console.WriteLine("Windows Terminal: Left={0}, Top={1}, Right={2}, Bottom={3}, W={4}, H={5}",
                rect.Left, rect.Top, rect.Right, rect.Bottom,
                rect.Right - rect.Left, rect.Bottom - rect.Top);
        } else {
            Console.WriteLine("Windows Terminal not found");
        }
    }
}
"@

[TerminalPos]::Check()
