Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class WinPos {
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    public static int targetPid;

    public static bool Callback(IntPtr hWnd, IntPtr lParam) {
        int pid;
        GetWindowThreadProcessId(hWnd, out pid);
        if (pid == targetPid) {
            StringBuilder sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, 256);
            string title = sb.ToString();
            if (title == "Input" || title == "Mask") {
                RECT rect;
                GetWindowRect(hWnd, out rect);
                Console.WriteLine("{0}: Left={1}, Top={2}, Right={3}, Bottom={4}, W={5}, H={6}",
                    title, rect.Left, rect.Top, rect.Right, rect.Bottom,
                    rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
        }
        return true;
    }
}
"@

$proc = Get-Process -Name 'Promptveil' -ErrorAction SilentlyContinue
if ($proc) {
    [WinPos]::targetPid = $proc.Id
    [WinPos]::EnumWindows([WinPos+EnumWindowsProc]::CreateDelegate([WinPos+EnumWindowsProc], [WinPos], "Callback"), [IntPtr]::Zero) | Out-Null
}

# Also check Windows Terminal position
$wt = Get-Process -Name 'WindowsTerminal' -ErrorAction SilentlyContinue
if ($wt) {
    Write-Host "`nWindows Terminal PID: $($wt.Id)"
}
