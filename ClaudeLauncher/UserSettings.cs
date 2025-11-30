using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeLauncher;

public class UserSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClaudeLauncher",
        "settings.json");

    private static UserSettings? _instance;
    private static readonly object _lock = new();

    public static UserSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= Load();
                }
            }
            return _instance;
        }
    }

    // Terminal settings
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TerminalType Terminal { get; set; } = TerminalType.WindowsTerminal;

    public string ClaudeArguments { get; set; } = "";

    // Hotkey settings
    public bool HotkeyCtrl { get; set; } = true;
    public bool HotkeyShift { get; set; } = true;
    public bool HotkeyAlt { get; set; } = false;
    public bool HotkeyWin { get; set; } = false;
    public string HotkeyKey { get; set; } = "C";

    // Theme
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ThemeMode Theme { get; set; } = ThemeMode.Dark;

    // Window position
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }

    // Pinned and custom folders
    public List<string> PinnedPaths { get; set; } = [];
    public List<CustomFolder> CustomFolders { get; set; } = [];

    // Launch history (most recent first)
    public List<string> LaunchHistory { get; set; } = [];
    public int MaxHistoryItems { get; set; } = 10;

    // IDE sources
    public bool IncludeVisualStudio { get; set; } = true;
    public bool IncludeVSCode { get; set; } = true;
    public bool IncludeRider { get; set; } = true;

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail if we can't save
        }
    }

    private static UserSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
        }
        catch
        {
            // Silently fail if we can't load
        }

        return new UserSettings();
    }

    public void AddToHistory(string path)
    {
        LaunchHistory.Remove(path);
        LaunchHistory.Insert(0, path);

        if (LaunchHistory.Count > MaxHistoryItems)
        {
            LaunchHistory = LaunchHistory.Take(MaxHistoryItems).ToList();
        }

        Save();
    }

    public void TogglePin(string path)
    {
        if (PinnedPaths.Contains(path))
        {
            PinnedPaths.Remove(path);
        }
        else
        {
            PinnedPaths.Add(path);
        }
        Save();
    }

    public bool IsPinned(string path) => PinnedPaths.Contains(path);
}

public enum TerminalType
{
    WindowsTerminal,
    PowerShell,
    Cmd
}

public enum ThemeMode
{
    Dark,
    Light,
    System
}

public class CustomFolder
{
    public string Path { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
