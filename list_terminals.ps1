# List all potential terminal processes
Write-Host "=== Terminal-like processes ==="
Get-Process | ForEach-Object {
    $name = $_.ProcessName
    if ($name -like "*terminal*" -or $name -like "*cmd*" -or $name -like "*powershell*" -or $name -like "*code*" -or $name -eq "wt" -or $name -like "*WindowsTerminal*") {
        Write-Host "PID=$($_.Id) Name=$name Title=$($_.MainWindowTitle)"
    }
}

Write-Host "`n=== Windows with CASCADIA class ==="
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class FindCascadia {
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    public static bool Callback(IntPtr hWnd, IntPtr lParam) {
        StringBuilder className = new StringBuilder(256);
        GetClassName(hWnd, className, 256);
        string cls = className.ToString();
        if (cls.Contains("CASCADIA") || cls.Contains("Terminal") || cls.Contains("Console")) {
            StringBuilder title = new StringBuilder(256);
            GetWindowText(hWnd, title, 256);
            bool visible = IsWindowVisible(hWnd);
            Console.WriteLine("Handle={0}, Class={1}, Title={2}, Visible={3}", hWnd, cls, title, visible);
        }
        return true;
    }
}
"@

[FindCascadia]::EnumWindows([FindCascadia+EnumWindowsProc]::CreateDelegate([FindCascadia+EnumWindowsProc], [FindCascadia], "Callback"), [IntPtr]::Zero) | Out-Null
