using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SelfPlusPlusCLI.Common;

public class LogDataService
{
    public const string LogDataFileName = "LogData.json";
    
    private readonly ILogger _logger;
    private List<LogEntry> _logEntries;
    private readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions 
    { 
        PropertyNamingPolicy = null,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
     };


    public LogDataService(ILogger<LogDataService> logger)
    {
        _logger = logger;

        // open data file
        _logEntries = ReadLogEntries();
    }

    public string GetLogDataFileDirectory()
    {
        string logDataFileDirectory;
        if (OperatingSystem.IsWindows())
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            logDataFileDirectory = Path.Combine(basePath, "SelfPlusPlus");
        }
        else if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            logDataFileDirectory = Path.Combine(home, "Library", "Application Support", "SelfPlusPlus");
        }
        else
        {
            var xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (string.IsNullOrWhiteSpace(xdg))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                logDataFileDirectory = Path.Combine(home, ".local", "share", "SelfPlusPlus");
            }
            else
            {
                logDataFileDirectory = Path.Combine(xdg, "SelfPlusPlus");
            }
        }

        return logDataFileDirectory;
    }
    
    public string GetLogDataFilePath()
    {
        return Path.Combine(GetLogDataFileDirectory(), LogDataFileName);
    }

    public List<LogEntry> ReadLogEntries()
    {
        var logDataFileDirectory = GetLogDataFileDirectory();
        if (!Directory.Exists(logDataFileDirectory)) Directory.CreateDirectory(logDataFileDirectory);

        var logDataFilePath = GetLogDataFilePath();
        if (!File.Exists(logDataFilePath)) return new List<LogEntry>();

        var raw = File.ReadAllText(logDataFilePath);
        if (string.IsNullOrWhiteSpace(raw)) return new List<LogEntry>();

        try
        {
            var list = JsonSerializer.Deserialize<List<LogEntry>>(raw, JsonOptions) ?? new List<LogEntry>();
            return list;
        }
        catch
        {
            // Attempt to read single object and wrap
            try
            {
                var single = JsonSerializer.Deserialize<LogEntry>(raw, JsonOptions);
                if (single != null) return new List<LogEntry> { single };
            }
            catch { }
            throw;
        }
    }
    
    public string ToJsonString()
    {
        return JsonSerializer.Serialize(_logEntries, JsonOptions);
    }
}