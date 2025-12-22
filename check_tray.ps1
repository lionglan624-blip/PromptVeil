# Check system tray icons by reading from registry and checking notification area
Write-Host "=== Checking Tray Icons ==="

# Method 1: Check running notify icon processes
Write-Host "`n--- Processes with potential tray icons ---"
$procs = @("Promptveil", "explorer")
foreach ($name in $procs) {
    $p = Get-Process -Name $name -ErrorAction SilentlyContinue
    if ($p) {
        Write-Host "$name : PID=$($p.Id)"
    }
}

# Method 2: Use UI Automation to check system tray
Write-Host "`n--- System Tray via UI Automation ---"
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

try {
    $root = [System.Windows.Automation.AutomationElement]::RootElement

    # Find taskbar
    $taskbar = $root.FindFirst(
        [System.Windows.Automation.TreeScope]::Children,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ClassNameProperty, "Shell_TrayWnd")))

    if ($taskbar) {
        Write-Host "Found taskbar"

        # Find system tray (notification area)
        $trayNotify = $taskbar.FindFirst(
            [System.Windows.Automation.TreeScope]::Descendants,
            (New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ClassNameProperty, "TrayNotifyWnd")))

        if ($trayNotify) {
            Write-Host "Found TrayNotifyWnd"

            # Get all buttons in tray
            $buttons = $trayNotify.FindAll(
                [System.Windows.Automation.TreeScope]::Descendants,
                (New-Object System.Windows.Automation.PropertyCondition(
                    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                    [System.Windows.Automation.ControlType]::Button)))

            Write-Host "Tray buttons found: $($buttons.Count)"
            foreach ($btn in $buttons) {
                $name = $btn.Current.Name
                if ($name) {
                    Write-Host "  - $name"
                }
            }
        }

        # Also check overflow area
        $overflow = $taskbar.FindFirst(
            [System.Windows.Automation.TreeScope]::Descendants,
            (New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ClassNameProperty, "NotifyIconOverflowWindow")))

        if ($overflow) {
            Write-Host "`nFound overflow area"
        }
    }
} catch {
    Write-Host "UI Automation error: $_"
}

# Method 3: Check if H.NotifyIcon created a window
Write-Host "`n--- Promptveil Windows ---"
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class TrayCheck {
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    public static int targetPid;

    public static bool Callback(IntPtr hWnd, IntPtr lParam) {
        int pid;
        GetWindowThreadProcessId(hWnd, out pid);
        if (pid == targetPid) {
            StringBuilder sb = new StringBuilder(256);
            GetClassName(hWnd, sb, 256);
            Console.WriteLine("  Class: " + sb.ToString());
        }
        return true;
    }
}
"@

$proc = Get-Process -Name 'Promptveil' -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "Promptveil window classes:"
    [TrayCheck]::targetPid = $proc.Id
    [TrayCheck]::EnumWindows([TrayCheck+EnumWindowsProc]::CreateDelegate([TrayCheck+EnumWindowsProc], [TrayCheck], "Callback"), [IntPtr]::Zero) | Out-Null
}
