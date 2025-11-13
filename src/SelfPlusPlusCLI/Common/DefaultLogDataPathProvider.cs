namespace SelfPlusPlusCLI.Common;

public sealed class DefaultLogDataPathProvider : ILogDataPathProvider
{
    public const string ApplicationDirectoryName = "SelfPlusPlus";
    public const string LogDataFileName = "LogData.json";

    public string GetLogDataDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(basePath, ApplicationDirectoryName);
        }

        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", ApplicationDirectoryName);
        }

        var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (!string.IsNullOrWhiteSpace(xdgDataHome))
        {
            return Path.Combine(xdgDataHome, ApplicationDirectoryName);
        }

        var fallbackHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(fallbackHome, ".local", "share", ApplicationDirectoryName);
    }

    public string GetLogDataFilePath()
    {
        return Path.Combine(GetLogDataDirectory(), LogDataFileName);
    }
}

