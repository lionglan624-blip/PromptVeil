using System.Windows;
using Promptveil.Helpers;

namespace Promptveil.Services;

/// <summary>
/// Handles text injection via clipboard
/// </summary>
public class ClipboardInjector
{
    private readonly int _pasteDelayMs;

    public ClipboardInjector(int pasteDelayMs = 50)
    {
        _pasteDelayMs = pasteDelayMs;
    }

    /// <summary>
    /// Injects text into target window via clipboard paste
    /// </summary>
    public async Task InjectTextAsync(IntPtr targetWindow, string text, bool sendEnter = true)
    {
        // Backup current clipboard
        string? backup = null;
        bool hadText = false;

        try
        {
            if (Clipboard.ContainsText())
            {
                hadText = true;
                backup = Clipboard.GetText();
            }
        }
        catch
        {
            // Clipboard access may fail
        }

        try
        {
            // Set text to clipboard
            Clipboard.SetText(text);

            // Activate target window
            NativeMethods.SetForegroundWindow(targetWindow);
            await Task.Delay(20);

            // Send Ctrl+V
            NativeMethods.SendCtrlV();

            // Wait for paste to complete
            await Task.Delay(_pasteDelayMs);

            // Send Enter if requested
            if (sendEnter)
            {
                NativeMethods.SendEnter();
                // Wait for Enter to be processed by terminal
                await Task.Delay(50);
            }
        }
        finally
        {
            // Restore original clipboard after a short delay
            await Task.Delay(50);

            try
            {
                if (hadText && backup != null)
                {
                    Clipboard.SetText(backup);
                }
                else if (!hadText)
                {
                    Clipboard.Clear();
                }
            }
            catch
            {
                // Clipboard restore may fail
            }
        }
    }
}
