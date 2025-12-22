using System.IO;
using System.Text.Json;
using Promptveil.Models;

namespace Promptveil.Services;

/// <summary>
/// Handles configuration persistence
/// </summary>
public class ConfigService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "terminal_input_overlay");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public Config Config { get; private set; } = new();

    public void Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                Config = JsonSerializer.Deserialize<Config>(json, JsonOptions) ?? new Config();
            }
        }
        catch
        {
            Config = new Config();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(Config, JsonOptions);
            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    public void AddHistory(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        // Remove duplicate if exists
        Config.History.Remove(command);

        // Add to end
        Config.History.Add(command);

        // Trim if exceeds max
        while (Config.History.Count > Config.MaxHistory)
        {
            Config.History.RemoveAt(0);
        }

        Save();
    }
}
