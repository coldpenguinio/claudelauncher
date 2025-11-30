using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;
using SelectionMode = System.Windows.Controls.SelectionMode;

namespace ClaudeLauncher;

public partial class SearchWindow : Window
{
    private readonly List<SolutionInfo> _allSolutions;
    private readonly ObservableCollection<SolutionInfo> _filteredSolutions;
    private readonly Action<SolutionInfo> _onLaunchClaude;
    private readonly Action? _onOpenSettings;
    private bool _isClosing;
    private bool _isMultiSelectMode;

    public SearchWindow(
        List<SolutionInfo> solutions,
        Action<SolutionInfo> onLaunchClaude,
        Action? onOpenSettings = null)
    {
        InitializeComponent();

        _allSolutions = solutions;
        _onLaunchClaude = onLaunchClaude;
        _onOpenSettings = onOpenSettings;
        _filteredSolutions = new ObservableCollection<SolutionInfo>(solutions);

        ResultsList.ItemsSource = _filteredSolutions;

        // Restore window position
        var settings = UserSettings.Instance;
        if (settings.WindowLeft.HasValue && settings.WindowTop.HasValue)
        {
            Left = settings.WindowLeft.Value;
            Top = settings.WindowTop.Value;
            WindowStartupLocation = WindowStartupLocation.Manual;
        }

        Loaded += (s, e) =>
        {
            SearchBox.Focus();
            if (_filteredSolutions.Count > 0)
            {
                ResultsList.SelectedIndex = 0;
            }
        };

        Closing += (s, e) =>
        {
            // Save window position
            settings.WindowLeft = Left;
            settings.WindowTop = Top;
            settings.Save();
        };
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = SearchBox.Text.Trim().ToLowerInvariant();

        _filteredSolutions.Clear();

        var filtered = string.IsNullOrEmpty(searchText)
            ? _allSolutions
            : _allSolutions.Where(s =>
                s.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                s.FullPath.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (s.GitBranch?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false));

        foreach (var solution in filtered)
        {
            _filteredSolutions.Add(solution);
        }

        if (_filteredSolutions.Count > 0)
        {
            ResultsList.SelectedIndex = 0;
        }
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                _isClosing = true;
                Close();
                e.Handled = true;
                break;

            case Key.Enter:
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    OpenInVisualStudio();
                }
                else if (_isMultiSelectMode && ResultsList.SelectedItems.Count > 1)
                {
                    LaunchMultiple();
                }
                else
                {
                    LaunchSelected();
                }
                e.Handled = true;
                break;

            case Key.Down:
                if (ResultsList.SelectedIndex < _filteredSolutions.Count - 1)
                {
                    ResultsList.SelectedIndex++;
                    ResultsList.ScrollIntoView(ResultsList.SelectedItem);
                }
                e.Handled = true;
                break;

            case Key.Up:
                if (ResultsList.SelectedIndex > 0)
                {
                    ResultsList.SelectedIndex--;
                    ResultsList.ScrollIntoView(ResultsList.SelectedItem);
                }
                e.Handled = true;
                break;

            case Key.P when Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                TogglePinSelected();
                e.Handled = true;
                break;

            case Key.C when Keyboard.Modifiers.HasFlag(ModifierKeys.Control) &&
                           Keyboard.Modifiers.HasFlag(ModifierKeys.Shift):
                CopyPathToClipboard();
                e.Handled = true;
                break;

            case Key.E when Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                OpenInExplorer();
                e.Handled = true;
                break;
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (!_isClosing)
        {
            _isClosing = true;
            Close();
        }
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isMultiSelectMode)
        {
            SearchBox.Focus();
        }
    }

    private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        LaunchSelected();
    }

    private void ResultsList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (ResultsList.SelectedItem is not SolutionInfo solution)
            return;

        var contextMenu = new ContextMenu();

        // Launch Claude
        var launchItem = new MenuItem { Header = "Launch Claude Here" };
        launchItem.Click += (s, args) => LaunchSelected();
        contextMenu.Items.Add(launchItem);

        // Launch with arguments
        var argsItem = new MenuItem { Header = "Launch with Arguments..." };
        argsItem.Click += (s, args) => LaunchWithArguments(solution);
        contextMenu.Items.Add(argsItem);

        contextMenu.Items.Add(new Separator());

        // Open in Visual Studio
        if (solution.FullPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
            solution.FullPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            var vsItem = new MenuItem { Header = "Open in Visual Studio" };
            vsItem.Click += (s, args) => OpenInVisualStudio();
            contextMenu.Items.Add(vsItem);
        }

        // Open in VS Code
        var codeItem = new MenuItem { Header = "Open in VS Code" };
        codeItem.Click += (s, args) => OpenInVSCode(solution);
        contextMenu.Items.Add(codeItem);

        // Open in Explorer
        var explorerItem = new MenuItem { Header = "Open Folder in Explorer" };
        explorerItem.Click += (s, args) => OpenInExplorer();
        contextMenu.Items.Add(explorerItem);

        contextMenu.Items.Add(new Separator());

        // Pin/Unpin
        var pinItem = new MenuItem
        {
            Header = solution.IsPinned ? "Unpin" : "Pin to Top"
        };
        pinItem.Click += (s, args) => TogglePinSelected();
        contextMenu.Items.Add(pinItem);

        // Copy path
        var copyPathItem = new MenuItem { Header = "Copy Path" };
        copyPathItem.Click += (s, args) => CopyPathToClipboard();
        contextMenu.Items.Add(copyPathItem);

        // Copy folder path
        var copyFolderItem = new MenuItem { Header = "Copy Folder Path" };
        copyFolderItem.Click += (s, args) => CopyFolderPathToClipboard();
        contextMenu.Items.Add(copyFolderItem);

        contextMenu.IsOpen = true;
    }

    private void MultiSelectCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _isMultiSelectMode = MultiSelectCheckBox.IsChecked ?? false;
        ResultsList.SelectionMode = _isMultiSelectMode
            ? SelectionMode.Extended
            : SelectionMode.Single;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _isClosing = true;
        Close();
        _onOpenSettings?.Invoke();
    }

    private void LaunchSelected()
    {
        if (ResultsList.SelectedItem is SolutionInfo solution)
        {
            UserSettings.Instance.AddToHistory(solution.FullPath);
            _isClosing = true;
            Close();
            _onLaunchClaude(solution);
        }
    }

    private void LaunchMultiple()
    {
        var selected = ResultsList.SelectedItems.Cast<SolutionInfo>().ToList();
        _isClosing = true;
        Close();

        foreach (var solution in selected)
        {
            UserSettings.Instance.AddToHistory(solution.FullPath);
            _onLaunchClaude(solution);
        }
    }

    private void LaunchWithArguments(SolutionInfo solution)
    {
        var dialog = new ArgumentsDialog(UserSettings.Instance.ClaudeArguments);
        dialog.Owner = this;

        if (dialog.ShowDialog() == true)
        {
            UserSettings.Instance.ClaudeArguments = dialog.Arguments;
            UserSettings.Instance.Save();
            UserSettings.Instance.AddToHistory(solution.FullPath);
            _isClosing = true;
            Close();
            _onLaunchClaude(solution);
        }
    }

    private void OpenInVisualStudio()
    {
        if (ResultsList.SelectedItem is not SolutionInfo solution)
            return;

        try
        {
            if (solution.FullPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                solution.FullPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = solution.FullPath,
                    UseShellExecute = true
                });
            }
            else
            {
                // Open folder in VS
                Process.Start(new ProcessStartInfo
                {
                    FileName = "devenv.exe",
                    Arguments = $"\"{solution.Directory}\"",
                    UseShellExecute = true
                });
            }

            _isClosing = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open Visual Studio: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenInVSCode(SolutionInfo solution)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "code",
                Arguments = $"\"{solution.Directory}\"",
                UseShellExecute = true
            });
            _isClosing = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open VS Code: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenInExplorer()
    {
        if (ResultsList.SelectedItem is not SolutionInfo solution)
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{solution.FullPath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open Explorer: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TogglePinSelected()
    {
        if (ResultsList.SelectedItem is not SolutionInfo solution)
            return;

        UserSettings.Instance.TogglePin(solution.FullPath);

        // Refresh the list to reflect the change
        var searchText = SearchBox.Text;
        SearchBox.Text = "";
        SearchBox.Text = searchText;
    }

    private void CopyPathToClipboard()
    {
        if (ResultsList.SelectedItem is SolutionInfo solution)
        {
            Clipboard.SetText(solution.FullPath);
        }
    }

    private void CopyFolderPathToClipboard()
    {
        if (ResultsList.SelectedItem is SolutionInfo solution)
        {
            Clipboard.SetText(solution.Directory);
        }
    }
}
