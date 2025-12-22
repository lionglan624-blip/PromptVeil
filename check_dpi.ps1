Add-Type @"
using System;
using System.Runtime.InteropServices;

public class DpiCheck {
    [DllImport("user32.dll")]
    public static extern uint GetDpiForSystem();

    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("shcore.dll")]
    public static extern int GetProcessDpiAwareness(IntPtr hprocess, out int awareness);
}
"@

Write-Host "System DPI: $([DpiCheck]::GetDpiForSystem())"

$hwnd = [IntPtr]10617830
$dpi = [DpiCheck]::GetDpiForWindow($hwnd)
Write-Host "Terminal window DPI: $dpi"

$awareness = 0
[DpiCheck]::GetProcessDpiAwareness([IntPtr]::Zero, [ref]$awareness) | Out-Null
Write-Host "Current process DPI awareness: $awareness (0=unaware, 1=system, 2=per-monitor)"

# Calculate expected scale
$scale = [DpiCheck]::GetDpiForSystem() / 96.0
Write-Host "Scale factor: $scale"

# Check if 289 * some_factor = 116
Write-Host "`nCoordinate check:"
Write-Host "289 / 2.5 = $(289 / 2.5)"
Write-Host "116 * 2.5 = $(116 * 2.5)"
