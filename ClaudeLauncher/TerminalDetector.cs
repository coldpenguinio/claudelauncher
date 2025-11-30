using System.Diagnostics;
using System.IO;

namespace ClaudeLauncher;

public static class TerminalDetector
{
    private static bool? _hasWindowsTerminal;
    private static bool? _hasPowerShell;
    private static bool? _hasCmd;

    public static bool HasWindowsTerminal
    {
        get
        {
            _hasWindowsTerminal ??= CheckCommandExists("wt.exe");
            return _hasWindowsTerminal.Value;
        }
    }

    public static bool HasPowerShell
    {
        get
        {
            _hasPowerShell ??= CheckCommandExists("powershell.exe");
            return _hasPowerShell.Value;
        }
    }

    public static bool HasCmd
    {
        get
        {
            _hasCmd ??= CheckCommandExists("cmd.exe");
            return _hasCmd.Value;
        }
    }

    public static TerminalType GetDefaultTerminal()
    {
        if (HasWindowsTerminal) return TerminalType.WindowsTerminal;
        if (HasPowerShell) return TerminalType.PowerShell;
        return TerminalType.Cmd;
    }

    public static TerminalType GetBestAvailable(TerminalType preferred)
    {
        return preferred switch
        {
            TerminalType.WindowsTerminal when HasWindowsTerminal => TerminalType.WindowsTerminal,
            TerminalType.PowerShell when HasPowerShell => TerminalType.PowerShell,
            TerminalType.Cmd when HasCmd => TerminalType.Cmd,
            _ => GetDefaultTerminal()
        };
    }

    public static void Refresh()
    {
        _hasWindowsTerminal = null;
        _hasPowerShell = null;
        _hasCmd = null;
    }

    private static bool CheckCommandExists(string command)
    {
        try
        {
            // Check if it's in PATH
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
            var paths = pathEnv.Split(Path.PathSeparator);

            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, command);
                if (File.Exists(fullPath))
                {
                    return true;
                }
            }

            // Also check common locations for Windows Terminal
            if (command == "wt.exe")
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var wtPath = Path.Combine(localAppData, "Microsoft", "WindowsApps", "wt.exe");
                if (File.Exists(wtPath))
                {
                    return true;
                }
            }

            // Try running "where" command as fallback
            var startInfo = new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForExit(1000);
                return process.ExitCode == 0;
            }
        }
        catch
        {
            // Ignore errors
        }

        return false;
    }
}
