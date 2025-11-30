using System.IO;
using System.Text.Json;

namespace ClaudeLauncher;

public class RecentSolutionsService
{
    private const int MaxRecentSolutions = 50;

    public List<SolutionInfo> GetRecentSolutions()
    {
        var solutions = new List<SolutionInfo>();
        var settings = UserSettings.Instance;

        // Get from Visual Studio
        if (settings.IncludeVisualStudio)
        {
            solutions.AddRange(GetVisualStudioSolutions());
        }

        // Get from VS Code
        if (settings.IncludeVSCode)
        {
            solutions.AddRange(GetVSCodeProjects());
        }

        // Get from Rider
        if (settings.IncludeRider)
        {
            solutions.AddRange(GetRiderProjects());
        }

        // Add custom folders
        foreach (var folder in settings.CustomFolders)
        {
            if (Directory.Exists(folder.Path))
            {
                solutions.Add(new SolutionInfo
                {
                    FullPath = folder.Path,
                    DisplayName = string.IsNullOrEmpty(folder.DisplayName)
                        ? Path.GetFileName(folder.Path)
                        : folder.DisplayName,
                    LastAccessed = Directory.GetLastWriteTime(folder.Path),
                    Source = ProjectSource.Custom
                });
            }
        }

        // Remove duplicates, prioritize pinned and history, then sort by last accessed
        return solutions
            .GroupBy(s => s.FullPath.ToLowerInvariant())
            .Select(g => g.OrderByDescending(s => s.LastAccessed).First())
            .OrderByDescending(s => s.IsPinned)
            .ThenByDescending(s => s.IsInHistory)
            .ThenByDescending(s => s.IsInHistory ? -s.HistoryIndex : 0)
            .ThenByDescending(s => s.LastAccessed)
            .Take(MaxRecentSolutions)
            .ToList();
    }

    private List<SolutionInfo> GetVisualStudioSolutions()
    {
        var solutions = new List<SolutionInfo>();
        var vsVersions = new[] { "17.0", "16.0", "15.0" };

        foreach (var version in vsVersions)
        {
            solutions.AddRange(GetSolutionsFromVSVersion(version));
        }

        return solutions;
    }

    private List<SolutionInfo> GetSolutionsFromVSVersion(string version)
    {
        var solutions = new List<SolutionInfo>();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var vsFolder = Path.Combine(localAppData, "Microsoft", "VisualStudio");

        if (!Directory.Exists(vsFolder))
            return solutions;

        var vsFolders = Directory.GetDirectories(vsFolder, $"{version}_*");

        foreach (var folder in vsFolders)
        {
            solutions.AddRange(GetFromCodeContainers(folder));
            solutions.AddRange(GetFromApplicationPrivateSettings(folder));
        }

        return solutions;
    }

    private List<SolutionInfo> GetFromCodeContainers(string vsFolder)
    {
        var solutions = new List<SolutionInfo>();
        var codeContainersPath = Path.Combine(vsFolder, "CodeContainers.json");

        if (!File.Exists(codeContainersPath))
            return solutions;

        try
        {
            var json = File.ReadAllText(codeContainersPath);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("CodeContainers", out var containers))
            {
                foreach (var container in containers.EnumerateArray())
                {
                    if (container.TryGetProperty("Value", out var value) &&
                        value.TryGetProperty("LocalProperties", out var localProps) &&
                        localProps.TryGetProperty("FullPath", out var fullPathElement))
                    {
                        var fullPath = fullPathElement.GetString();
                        if (!string.IsNullOrEmpty(fullPath) &&
                            (fullPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                             fullPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)))
                        {
                            var lastAccessed = DateTime.MinValue;
                            if (value.TryGetProperty("MRUTime", out var mruTime) &&
                                mruTime.TryGetInt64(out var ticks))
                            {
                                lastAccessed = DateTime.FromFileTimeUtc(ticks);
                            }

                            solutions.Add(new SolutionInfo
                            {
                                FullPath = fullPath,
                                DisplayName = GetDisplayName(fullPath),
                                LastAccessed = lastAccessed,
                                Source = ProjectSource.VisualStudio
                            });
                        }
                    }
                }
            }
        }
        catch
        {
            // Silently fail
        }

        return solutions;
    }

    private List<SolutionInfo> GetFromApplicationPrivateSettings(string vsFolder)
    {
        var solutions = new List<SolutionInfo>();
        var settingsPath = Path.Combine(vsFolder, "ApplicationPrivateSettings.xml");

        if (!File.Exists(settingsPath))
            return solutions;

        try
        {
            var xml = File.ReadAllText(settingsPath);
            var startIndex = 0;

            while ((startIndex = xml.IndexOf(".sln", startIndex, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                var pathStart = xml.LastIndexOf('"', startIndex);
                var pathEnd = xml.IndexOf('"', startIndex);

                if (pathStart != -1 && pathEnd != -1 && pathEnd > pathStart)
                {
                    var path = xml.Substring(pathStart + 1, pathEnd - pathStart - 1);
                    path = System.Net.WebUtility.HtmlDecode(path);

                    if (File.Exists(path))
                    {
                        solutions.Add(new SolutionInfo
                        {
                            FullPath = path,
                            DisplayName = GetDisplayName(path),
                            LastAccessed = File.GetLastWriteTime(path),
                            Source = ProjectSource.VisualStudio
                        });
                    }
                }
                startIndex++;
            }
        }
        catch
        {
            // Silently fail
        }

        return solutions;
    }

    private List<SolutionInfo> GetVSCodeProjects()
    {
        var projects = new List<SolutionInfo>();
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // VS Code stores recent workspaces in storage.json
        var storagePaths = new[]
        {
            Path.Combine(appData, "Code", "storage.json"),
            Path.Combine(appData, "Code - Insiders", "storage.json"),
            Path.Combine(appData, "VSCodium", "storage.json")
        };

        foreach (var storagePath in storagePaths)
        {
            if (!File.Exists(storagePath))
                continue;

            try
            {
                var json = File.ReadAllText(storagePath);
                using var doc = JsonDocument.Parse(json);

                // Try openedPathsList.entries (newer format)
                if (doc.RootElement.TryGetProperty("openedPathsList", out var pathsList) &&
                    pathsList.TryGetProperty("entries", out var entries))
                {
                    foreach (var entry in entries.EnumerateArray())
                    {
                        string? folderPath = null;

                        if (entry.TryGetProperty("folderUri", out var folderUri))
                        {
                            folderPath = UriToPath(folderUri.GetString());
                        }
                        else if (entry.TryGetProperty("workspace", out var workspace) &&
                                 workspace.TryGetProperty("configPath", out var configPath))
                        {
                            folderPath = UriToPath(configPath.GetString());
                        }

                        if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                        {
                            projects.Add(new SolutionInfo
                            {
                                FullPath = folderPath,
                                DisplayName = Path.GetFileName(folderPath),
                                LastAccessed = Directory.GetLastWriteTime(folderPath),
                                Source = ProjectSource.VSCode
                            });
                        }
                    }
                }
            }
            catch
            {
                // Silently fail
            }
        }

        return projects;
    }

    private List<SolutionInfo> GetRiderProjects()
    {
        var projects = new List<SolutionInfo>();
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var jetBrainsFolder = Path.Combine(appData, "JetBrains");

        if (!Directory.Exists(jetBrainsFolder))
            return projects;

        // Find Rider folders (e.g., Rider2023.3, Rider2024.1)
        var riderFolders = Directory.GetDirectories(jetBrainsFolder, "Rider*");

        foreach (var riderFolder in riderFolders)
        {
            var recentProjectsPath = Path.Combine(riderFolder, "options", "recentProjects.xml");
            if (!File.Exists(recentProjectsPath))
                continue;

            try
            {
                var xml = File.ReadAllText(recentProjectsPath);

                // Parse the XML to find project paths
                // Format: <entry key="$PROJECT_PATH$">
                var keyPattern = "key=\"";
                var startIndex = 0;

                while ((startIndex = xml.IndexOf(keyPattern, startIndex)) != -1)
                {
                    startIndex += keyPattern.Length;
                    var endIndex = xml.IndexOf('"', startIndex);

                    if (endIndex > startIndex)
                    {
                        var path = xml.Substring(startIndex, endIndex - startIndex);
                        path = path.Replace("$USER_HOME$", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

                        // Rider stores paths with forward slashes
                        path = path.Replace('/', Path.DirectorySeparatorChar);

                        if (Directory.Exists(path) || File.Exists(path))
                        {
                            var displayName = Path.GetFileName(path);
                            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                            {
                                displayName = Path.GetFileNameWithoutExtension(path);
                            }

                            projects.Add(new SolutionInfo
                            {
                                FullPath = path,
                                DisplayName = displayName,
                                LastAccessed = Directory.Exists(path)
                                    ? Directory.GetLastWriteTime(path)
                                    : File.GetLastWriteTime(path),
                                Source = ProjectSource.Rider
                            });
                        }
                    }
                }
            }
            catch
            {
                // Silently fail
            }
        }

        return projects;
    }

    private static string? UriToPath(string? uri)
    {
        if (string.IsNullOrEmpty(uri))
            return null;

        try
        {
            if (uri.StartsWith("file:///"))
            {
                // file:///c%3A/Users/... -> C:\Users\...
                var path = Uri.UnescapeDataString(uri.Substring(8));
                return path.Replace('/', Path.DirectorySeparatorChar);
            }
        }
        catch
        {
            // Ignore invalid URIs
        }

        return null;
    }

    private static string GetDisplayName(string fullPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(fullPath);
        var directory = Path.GetDirectoryName(fullPath);
        var parentFolder = directory != null ? Path.GetFileName(directory) : "";

        if (!string.IsNullOrEmpty(parentFolder) &&
            !parentFolder.Equals(fileName, StringComparison.OrdinalIgnoreCase))
        {
            return $"{fileName} ({parentFolder})";
        }

        return fileName ?? fullPath;
    }
}
