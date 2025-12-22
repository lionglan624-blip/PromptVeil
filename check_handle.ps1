Add-Type @"
using System;
using System.Runtime.InteropServices;

public class HandleCheck {
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
}
"@

$hwnd = [HandleCheck]::FindWindow("CASCADIA_HOSTING_WINDOW_CLASS", $null)
Write-Host "Handle from FindWindow: $hwnd"

$rect = New-Object HandleCheck+RECT
[HandleCheck]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
Write-Host "Rect: Left=$($rect.Left), Top=$($rect.Top), Right=$($rect.Right), Bottom=$($rect.Bottom)"
Write-Host "Size: W=$($rect.Right - $rect.Left), H=$($rect.Bottom - $rect.Top)"

# Also check specific handle 10617830
$specificHandle = [IntPtr]10617830
$rect2 = New-Object HandleCheck+RECT
[HandleCheck]::GetWindowRect($specificHandle, [ref]$rect2) | Out-Null
Write-Host "`nHandle 10617830:"
Write-Host "Rect: Left=$($rect2.Left), Top=$($rect2.Top), Right=$($rect2.Right), Bottom=$($rect2.Bottom)"
