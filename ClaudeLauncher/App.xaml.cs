using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace ClaudeLauncher;

public partial class App : Application
{
    private NotifyIcon? _notifyIcon;
    private RecentSolutionsService? _recentSolutionsService;
    private GlobalHotkey? _hotkey;
    private SearchWindow? _searchWindow;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _recentSolutionsService = new RecentSolutionsService();

        InitializeTrayIcon();
        InitializeHotkey();
    }

    private void InitializeTrayIcon()
    {
        var settings = UserSettings.Instance;
        var hotkeyText = BuildHotkeyText(settings);

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Visible = true,
            Text = $"Claude Launcher ({hotkeyText})"
        };

        _notifyIcon.ContextMenuStrip = BuildContextMenu();
        _notifyIcon.DoubleClick += (s, e) => ShowSearchWindow();
    }

    private static string BuildHotkeyText(UserSettings settings)
    {
        var parts = new List<string>();
        if (settings.HotkeyCtrl) parts.Add("Ctrl");
        if (settings.HotkeyShift) parts.Add("Shift");
        if (settings.HotkeyAlt) parts.Add("Alt");
        if (settings.HotkeyWin) parts.Add("Win");
        parts.Add(settings.HotkeyKey);
        return string.Join("+", parts);
    }

    private void InitializeHotkey()
    {
        try
        {
            var settings = UserSettings.Instance;

            uint modifiers = GlobalHotkey.MOD_NOREPEAT;
            if (settings.HotkeyCtrl) modifiers |= GlobalHotkey.MOD_CONTROL;
            if (settings.HotkeyShift) modifiers |= GlobalHotkey.MOD_SHIFT;
            if (settings.HotkeyAlt) modifiers |= GlobalHotkey.MOD_ALT;
            if (settings.HotkeyWin) modifiers |= GlobalHotkey.MOD_WIN;

            uint vk = (uint)settings.HotkeyKey.ToUpper()[0];

            _hotkey = new GlobalHotkey(modifiers, vk);
            _hotkey.HotkeyPressed += (s, e) => ShowSearchWindow();
        }
        catch (InvalidOperationException)
        {
            var settings = UserSettings.Instance;
            var hotkeyText = BuildHotkeyText(settings);
            System.Windows.MessageBox.Show(
                $"Could not register global hotkey {hotkeyText}. It may be in use by another application.",
                "Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void ShowSearchWindow()
    {
        if (_searchWindow != null && _searchWindow.IsVisible)
        {
            _searchWindow.Activate();
            return;
        }

        var solutions = _recentSolutionsService?.GetRecentSolutions() ?? [];

        _searchWindow = new SearchWindow(solutions, LaunchClaude, ShowSettings);
        _searchWindow.Closed += (s, e) => _searchWindow = null;
        _searchWindow.Show();
        _searchWindow.Activate();
    }

    private void ShowSettings()
    {
        if (_settingsWindow != null && _settingsWindow.IsVisible)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(OnSettingsChanged);
        _settingsWindow.Closed += (s, e) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void OnSettingsChanged()
    {
        // Update tray icon tooltip
        var settings = UserSettings.Instance;
        var hotkeyText = BuildHotkeyText(settings);
        if (_notifyIcon != null)
        {
            _notifyIcon.Text = $"Claude Launcher ({hotkeyText})";
        }

        // Refresh context menu
        RefreshMenu();
    }

    private Icon LoadIcon()
    {
        try
        {
            var resourceStream = GetResourceStream(new Uri("pack://application:,,,/Resources/app.ico"));
            if (resourceStream != null)
            {
                return new Icon(resourceStream.Stream);
            }
        }
        catch
        {
            // Resource not found, use fallback
        }

        // Fallback to generated icon
        return IconGenerator.CreateIcon();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        PopulateMenu(menu);
        return menu;
    }

    private void PopulateMenu(ContextMenuStrip menu)
    {
        menu.Items.Clear();

        var settings = UserSettings.Instance;
        var hotkeyText = BuildHotkeyText(settings);

        // Search option at top
        var searchItem = new ToolStripMenuItem($"Search Solutions... ({hotkeyText})");
        searchItem.Click += (s, e) => ShowSearchWindow();
        searchItem.Font = new Font(menu.Font, System.Drawing.FontStyle.Bold);
        menu.Items.Add(searchItem);

        menu.Items.Add(new ToolStripSeparator());

        var solutions = _recentSolutionsService?.GetRecentSolutions() ?? [];

        if (solutions.Count == 0)
        {
            var noSolutionsItem = new ToolStripMenuItem("No recent solutions found")
            {
                Enabled = false
            };
            menu.Items.Add(noSolutionsItem);
        }
        else
        {
            // Show first 10 in the menu
            foreach (var solution in solutions.Take(10))
            {
                var displayText = solution.IsPinned ? $"* {solution.DisplayName}" : solution.DisplayName;
                var solutionItem = new ToolStripMenuItem(displayText)
                {
                    ToolTipText = solution.FullPath
                };
                solutionItem.Click += (s, e) => LaunchClaude(solution);
                menu.Items.Add(solutionItem);
            }

            if (solutions.Count > 10)
            {
                var moreItem = new ToolStripMenuItem($"... and {solutions.Count - 10} more (use Search)")
                {
                    Enabled = false
                };
                menu.Items.Add(moreItem);
            }
        }

        menu.Items.Add(new ToolStripSeparator());

        // Settings
        var settingsItem = new ToolStripMenuItem("Settings...");
        settingsItem.Click += (s, e) => ShowSettings();
        menu.Items.Add(settingsItem);

        var refreshItem = new ToolStripMenuItem("Refresh");
        refreshItem.Click += (s, e) => RefreshMenu();
        menu.Items.Add(refreshItem);

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => ExitApplication();
        menu.Items.Add(exitItem);
    }

    private void RefreshMenu()
    {
        if (_notifyIcon?.ContextMenuStrip != null)
        {
            PopulateMenu(_notifyIcon.ContextMenuStrip);
        }
    }

    private void LaunchClaude(SolutionInfo solution)
    {
        try
        {
            var directory = Path.GetDirectoryName(solution.FullPath);
            if (string.IsNullOrEmpty(directory))
            {
                // For custom folders, use the path directly
                directory = solution.FullPath;
            }

            if (!Directory.Exists(directory))
            {
                System.Windows.MessageBox.Show(
                    $"Directory not found: {directory}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var settings = UserSettings.Instance;
            var args = string.IsNullOrWhiteSpace(settings.ClaudeArguments)
                ? ""
                : $" {settings.ClaudeArguments}";

            ProcessStartInfo startInfo = settings.Terminal switch
            {
                TerminalType.WindowsTerminal => new ProcessStartInfo
                {
                    FileName = "wt.exe",
                    Arguments = $"-d \"{directory}\" cmd /c \"claude{args}\"",
                    UseShellExecute = true
                },
                TerminalType.PowerShell => new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoExit -Command \"cd '{directory}'; claude{args}\"",
                    UseShellExecute = true
                },
                _ => new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k \"cd /d \"{directory}\" && claude{args}\"",
                    UseShellExecute = true
                }
            };

            Process.Start(startInfo);
            UserSettings.Instance.AddToHistory(solution.FullPath);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to launch Claude: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ExitApplication()
    {
        _hotkey?.Dispose();

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkey?.Dispose();

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        base.OnExit(e);
    }
}
