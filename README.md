# ClaudeLauncher

A Windows system tray application that shows your recent Visual Studio, VS Code, and Rider projects, allowing you to quickly launch Claude Code in any project directory.

## Features

- **System Tray Integration**: Lives in your system tray for quick access
- **Multi-IDE Support**: Shows recent projects from Visual Studio, VS Code, and JetBrains Rider
- **Global Hotkey**: Press `Ctrl+Shift+C` (customizable) to open the launcher from anywhere
- **Search/Filter**: Quickly find projects by typing
- **Pin Favorites**: Pin frequently used projects to the top
- **Git Integration**: Shows current branch for each project
- **Multiple Terminals**: Launch in Windows Terminal, PowerShell, or CMD
- **Custom Arguments**: Launch Claude with custom arguments (e.g., `--resume`, `--continue`)
- **Custom Folders**: Add any folder to your project list
- **Dark/Light Theme**: Follows system theme or set manually
- **Multi-Select**: Launch Claude in multiple directories at once

## Installation

### Prerequisites
- .NET 8.0 SDK or later
- Windows 10/11
- Claude Code CLI installed and in PATH

### Build from Source
```bash
git clone https://github.com/yourusername/ClaudeLauncher.git
cd ClaudeLauncher
dotnet build
```

### Run
```bash
dotnet run --project ClaudeLauncher
```

### Publish as Single Executable
```bash
dotnet publish ClaudeLauncher -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

The executable will be in `ClaudeLauncher/bin/Release/net8.0-windows/win-x64/publish/`

## Usage

### Basic Usage
1. Launch ClaudeLauncher - it appears in your system tray
2. Press `Ctrl+Shift+C` or click the tray icon to open the launcher
3. Type to filter projects or use arrow keys to navigate
4. Press `Enter` to launch Claude in the selected project directory

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Ctrl+Shift+C` | Open launcher (global, customizable) |
| `Enter` | Launch Claude in selected directory |
| `Shift+Enter` | Open project in Visual Studio |
| `Esc` | Close launcher |
| `Ctrl+P` | Toggle pin on selected project |
| `Ctrl+E` | Open folder in Explorer |
| `Ctrl+Shift+C` | Copy path to clipboard (when launcher is open) |
| `Up/Down` | Navigate project list |

### Right-Click Menu
Right-click any project for additional options:
- Launch Claude Here
- Launch with Arguments...
- Open in Visual Studio
- Open in VS Code
- Open Folder in Explorer
- Pin/Unpin
- Copy Path
- Copy Folder Path

### Settings
Click "Settings" in the launcher or right-click the tray icon to access:

- **Terminal**: Choose Windows Terminal, PowerShell, or CMD
- **Default Arguments**: Set default Claude CLI arguments
- **Hotkey**: Customize the global shortcut
- **Theme**: Dark, Light, or Follow System
- **Project Sources**: Toggle VS, VS Code, Rider on/off
- **Custom Folders**: Add folders that aren't from an IDE
- **Start with Windows**: Auto-start on login

### Multi-Select Mode
1. Check the "Multi" checkbox in the search window
2. Ctrl+Click to select multiple projects
3. Press Enter to launch Claude in all selected directories

## Configuration

Settings are stored in:
```
%APPDATA%\ClaudeLauncher\settings.json
```

## Project Structure

```
ClaudeLauncher/
├── ClaudeLauncher.slnx
├── README.md
└── ClaudeLauncher/
    ├── ClaudeLauncher.csproj
    ├── App.xaml / App.xaml.cs          # Main application, tray icon
    ├── SearchWindow.xaml / .cs          # Main search/launcher window
    ├── SettingsWindow.xaml / .cs        # Settings dialog
    ├── ArgumentsDialog.xaml / .cs       # Launch with arguments dialog
    ├── SolutionInfo.cs                  # Project data model
    ├── RecentSolutionsService.cs        # Reads recent projects from IDEs
    ├── UserSettings.cs                  # Persisted settings
    ├── AutoStartService.cs              # Windows auto-start registry
    ├── GlobalHotkey.cs                  # System-wide hotkey registration
    ├── ThemeManager.cs                  # Dark/light theme support
    ├── IconGenerator.cs                 # Programmatic icon generation
    └── Converters.cs                    # WPF value converters
```

## License

MIT
