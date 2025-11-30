using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using DialogResult = System.Windows.Forms.DialogResult;

namespace ClaudeLauncher;

public partial class SettingsWindow : Window
{
    private readonly ObservableCollection<CustomFolder> _customFolders;
    private readonly Action? _onSettingsChanged;

    public SettingsWindow(Action? onSettingsChanged = null)
    {
        InitializeComponent();
        _onSettingsChanged = onSettingsChanged;

        var settings = UserSettings.Instance;

        // Terminal
        TerminalWT.IsChecked = settings.Terminal == TerminalType.WindowsTerminal;
        TerminalPS.IsChecked = settings.Terminal == TerminalType.PowerShell;
        TerminalCmd.IsChecked = settings.Terminal == TerminalType.Cmd;
        DefaultArgsBox.Text = settings.ClaudeArguments;

        // Hotkey
        HotkeyCtrl.IsChecked = settings.HotkeyCtrl;
        HotkeyShift.IsChecked = settings.HotkeyShift;
        HotkeyAlt.IsChecked = settings.HotkeyAlt;
        HotkeyWin.IsChecked = settings.HotkeyWin;
        HotkeyKeyBox.Text = settings.HotkeyKey;

        // Theme
        ThemeDark.IsChecked = settings.Theme == ThemeMode.Dark;
        ThemeLight.IsChecked = settings.Theme == ThemeMode.Light;
        ThemeSystem.IsChecked = settings.Theme == ThemeMode.System;

        // IDE Sources
        IncludeVS.IsChecked = settings.IncludeVisualStudio;
        IncludeVSCode.IsChecked = settings.IncludeVSCode;
        IncludeRider.IsChecked = settings.IncludeRider;

        // Custom Folders
        _customFolders = new ObservableCollection<CustomFolder>(settings.CustomFolders);
        CustomFoldersList.ItemsSource = _customFolders;

        // Auto-start
        AutoStartCheckBox.IsChecked = AutoStartService.IsAutoStartEnabled();
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select a folder to add to the launcher"
        };

        var result = dialog.ShowDialog();
        if (result == System.Windows.Forms.DialogResult.OK)
        {
            var path = dialog.SelectedPath;
            if (!_customFolders.Any(f => f.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
            {
                _customFolders.Add(new CustomFolder
                {
                    Path = path,
                    DisplayName = System.IO.Path.GetFileName(path)
                });
            }
        }
    }

    private void RemoveFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string path)
        {
            var folder = _customFolders.FirstOrDefault(f => f.Path == path);
            if (folder != null)
            {
                _customFolders.Remove(folder);
            }
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var settings = UserSettings.Instance;

        // Terminal
        settings.Terminal = TerminalWT.IsChecked == true ? TerminalType.WindowsTerminal :
                           TerminalPS.IsChecked == true ? TerminalType.PowerShell :
                           TerminalType.Cmd;
        settings.ClaudeArguments = DefaultArgsBox.Text;

        // Hotkey
        settings.HotkeyCtrl = HotkeyCtrl.IsChecked ?? false;
        settings.HotkeyShift = HotkeyShift.IsChecked ?? false;
        settings.HotkeyAlt = HotkeyAlt.IsChecked ?? false;
        settings.HotkeyWin = HotkeyWin.IsChecked ?? false;
        settings.HotkeyKey = string.IsNullOrEmpty(HotkeyKeyBox.Text) ? "C" : HotkeyKeyBox.Text.ToUpper();

        // Theme
        settings.Theme = ThemeDark.IsChecked == true ? ThemeMode.Dark :
                        ThemeLight.IsChecked == true ? ThemeMode.Light :
                        ThemeMode.System;

        // IDE Sources
        settings.IncludeVisualStudio = IncludeVS.IsChecked ?? true;
        settings.IncludeVSCode = IncludeVSCode.IsChecked ?? true;
        settings.IncludeRider = IncludeRider.IsChecked ?? true;

        // Custom Folders
        settings.CustomFolders = _customFolders.ToList();

        // Auto-start
        AutoStartService.SetAutoStart(AutoStartCheckBox.IsChecked ?? false);

        settings.Save();
        _onSettingsChanged?.Invoke();
        Close();
    }
}
