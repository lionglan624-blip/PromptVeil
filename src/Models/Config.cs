using System.Text.Json;
using System.Text.Json.Serialization;

namespace Promptveil.Models;

/// <summary>
/// Application configuration
/// </summary>
public class Config
{
    /// <summary>
    /// Target process name (without .exe)
    /// </summary>
    [JsonPropertyName("target_process")]
    public string TargetProcess { get; set; } = "WindowsTerminal";

    /// <summary>
    /// X offset from terminal window left edge (pixels)
    /// </summary>
    [JsonPropertyName("offset_x")]
    public int OffsetX { get; set; } = 10;

    /// <summary>
    /// Y offset from terminal window bottom edge (pixels, negative = up from bottom)
    /// </summary>
    [JsonPropertyName("offset_y")]
    public int OffsetY { get; set; } = -60;

    /// <summary>
    /// Mask height in lines (1-5)
    /// </summary>
    [JsonPropertyName("mask_lines")]
    public int MaskLines { get; set; } = 2;

    /// <summary>
    /// Line height in pixels for mask calculation
    /// </summary>
    [JsonPropertyName("line_height_px")]
    public int LineHeightPx { get; set; } = 20;

    /// <summary>
    /// Input field height in pixels
    /// </summary>
    [JsonPropertyName("input_height_px")]
    public int InputHeightPx { get; set; } = 28;

    /// <summary>
    /// Font size for input field
    /// </summary>
    [JsonPropertyName("font_size")]
    public double FontSize { get; set; } = 14;

    /// <summary>
    /// Send empty Enter when input is empty
    /// </summary>
    [JsonPropertyName("send_empty_enter")]
    public bool SendEmptyEnter { get; set; } = false;

    /// <summary>
    /// Delay in ms after paste before sending Enter
    /// </summary>
    [JsonPropertyName("paste_delay_ms")]
    public int PasteDelayMs { get; set; } = 50;

    /// <summary>
    /// Window tracking poll interval in ms (when not using WinEventHook)
    /// </summary>
    [JsonPropertyName("poll_interval_ms")]
    public int PollIntervalMs { get; set; } = 50;

    /// <summary>
    /// Maximum history entries to keep
    /// </summary>
    [JsonPropertyName("max_history")]
    public int MaxHistory { get; set; } = 100;

    /// <summary>
    /// Command history
    /// </summary>
    [JsonPropertyName("history")]
    public List<string> History { get; set; } = new();

    /// <summary>
    /// Last known terminal window class name
    /// </summary>
    [JsonPropertyName("terminal_class")]
    public string TerminalClass { get; set; } = "CASCADIA_HOSTING_WINDOW_CLASS";
}
