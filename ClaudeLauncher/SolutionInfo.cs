using System.IO;

namespace ClaudeLauncher;

public enum ProjectSource
{
    VisualStudio,
    VSCode,
    Rider,
    Custom
}

public class SolutionInfo
{
    public required string FullPath { get; init; }
    public required string DisplayName { get; init; }
    public DateTime LastAccessed { get; init; }
    public ProjectSource Source { get; init; } = ProjectSource.VisualStudio;

    // Computed properties
    public string Directory => Path.GetDirectoryName(FullPath) ?? "";
    public bool Exists => File.Exists(FullPath) || System.IO.Directory.Exists(FullPath);
    public bool IsPinned => UserSettings.Instance.IsPinned(FullPath);
    public bool IsInHistory => UserSettings.Instance.LaunchHistory.Contains(FullPath);
    public int HistoryIndex => UserSettings.Instance.LaunchHistory.IndexOf(FullPath);

    public string? GitBranch
    {
        get
        {
            try
            {
                var gitDir = FindGitDirectory(Directory);
                if (gitDir == null) return null;

                var headFile = Path.Combine(gitDir, "HEAD");
                if (!File.Exists(headFile)) return null;

                var headContent = File.ReadAllText(headFile).Trim();
                if (headContent.StartsWith("ref: refs/heads/"))
                {
                    return headContent.Substring("ref: refs/heads/".Length);
                }

                // Detached HEAD - return short hash
                return headContent.Length > 7 ? headContent[..7] : headContent;
            }
            catch
            {
                return null;
            }
        }
    }

    public bool HasUncommittedChanges
    {
        get
        {
            try
            {
                var gitDir = FindGitDirectory(Directory);
                if (gitDir == null) return false;

                var indexFile = Path.Combine(gitDir, "index");
                if (!File.Exists(indexFile)) return false;

                // Simple heuristic: check if index was modified recently
                var indexTime = File.GetLastWriteTime(indexFile);
                return indexTime > DateTime.Now.AddMinutes(-5);
            }
            catch
            {
                return false;
            }
        }
    }

    public string LastModifiedText
    {
        get
        {
            if (!Exists) return "Not found";

            var lastWrite = File.Exists(FullPath)
                ? File.GetLastWriteTime(FullPath)
                : System.IO.Directory.GetLastWriteTime(FullPath);

            var diff = DateTime.Now - lastWrite;

            return diff.TotalMinutes < 1 ? "Just now" :
                   diff.TotalHours < 1 ? $"{(int)diff.TotalMinutes}m ago" :
                   diff.TotalDays < 1 ? $"{(int)diff.TotalHours}h ago" :
                   diff.TotalDays < 7 ? $"{(int)diff.TotalDays}d ago" :
                   lastWrite.ToString("MMM d");
        }
    }

    public string SourceIcon => Source switch
    {
        ProjectSource.VisualStudio => "VS",
        ProjectSource.VSCode => "Code",
        ProjectSource.Rider => "Rider",
        ProjectSource.Custom => "Folder",
        _ => ""
    };

    private static string? FindGitDirectory(string startPath)
    {
        var current = startPath;
        while (!string.IsNullOrEmpty(current))
        {
            var gitDir = Path.Combine(current, ".git");
            if (System.IO.Directory.Exists(gitDir))
            {
                return gitDir;
            }

            var parent = Path.GetDirectoryName(current);
            if (parent == current) break;
            current = parent;
        }
        return null;
    }
}
