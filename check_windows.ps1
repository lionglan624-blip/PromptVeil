Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

public class WindowFinder {
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    public static List<string> windows = new List<string>();
    public static int targetPid;

    public static bool Callback(IntPtr hWnd, IntPtr lParam) {
        int pid;
        GetWindowThreadProcessId(hWnd, out pid);
        if (pid == targetPid) {
            StringBuilder sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, 256);
            bool visible = IsWindowVisible(hWnd);
            windows.Add(String.Format("Handle={0}, Title={1}, Visible={2}", hWnd, sb.ToString(), visible));
        }
        return true;
    }
}
"@

$proc = Get-Process -Name 'Promptveil' -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "Found Promptveil process: PID=$($proc.Id)"
    [WindowFinder]::targetPid = $proc.Id
    [WindowFinder]::EnumWindows([WindowFinder+EnumWindowsProc]::CreateDelegate([WindowFinder+EnumWindowsProc], [WindowFinder], "Callback"), [IntPtr]::Zero) | Out-Null
    Write-Host "Windows found: $([WindowFinder]::windows.Count)"
    [WindowFinder]::windows | ForEach-Object { Write-Host $_ }
} else {
    Write-Host "Promptveil not running"
}
