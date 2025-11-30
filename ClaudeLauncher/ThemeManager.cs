using System.ComponentModel;
using System.Windows.Media;
using Microsoft.Win32;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace ClaudeLauncher;

public class ThemeManager : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _isDarkMode;

    public ThemeManager()
    {
        UpdateTheme();
        SystemEvents.UserPreferenceChanged += (s, e) =>
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                UpdateTheme();
            }
        };
    }

    private void UpdateTheme()
    {
        var settings = UserSettings.Instance;
        _isDarkMode = settings.Theme switch
        {
            ThemeMode.Dark => true,
            ThemeMode.Light => false,
            ThemeMode.System => IsSystemDarkMode(),
            _ => true
        };

        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(SearchBoxColor));
        OnPropertyChanged(nameof(BorderColor));
        OnPropertyChanged(nameof(PrimaryTextColor));
        OnPropertyChanged(nameof(SecondaryTextColor));
    }

    public void Refresh() => UpdateTheme();

    private static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 0;
        }
        catch
        {
            return true; // Default to dark
        }
    }

    public Brush BackgroundColor => new SolidColorBrush(
        _isDarkMode ? Color.FromRgb(0x1E, 0x1E, 0x1E) : Color.FromRgb(0xF3, 0xF3, 0xF3));

    public Brush SearchBoxColor => new SolidColorBrush(
        _isDarkMode ? Color.FromRgb(0x2D, 0x2D, 0x2D) : Color.FromRgb(0xFF, 0xFF, 0xFF));

    public Brush BorderColor => new SolidColorBrush(
        _isDarkMode ? Color.FromRgb(0x3C, 0x3C, 0x3C) : Color.FromRgb(0xCC, 0xCC, 0xCC));

    public Brush PrimaryTextColor => new SolidColorBrush(
        _isDarkMode ? Color.FromRgb(0xE0, 0xE0, 0xE0) : Color.FromRgb(0x1E, 0x1E, 0x1E));

    public Brush SecondaryTextColor => new SolidColorBrush(
        _isDarkMode ? Color.FromRgb(0x80, 0x80, 0x80) : Color.FromRgb(0x60, 0x60, 0x60));

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
