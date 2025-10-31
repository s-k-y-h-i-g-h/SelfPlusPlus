using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;


namespace SelfPlusPlusCLI.Common;

public class LogDataService
{
    public const string LogDataFileName = "LogData.json";
    
    private readonly ILogger _logger;
    private List<JObject> _logEntries;


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

    public List<JObject> ReadLogEntries()
    {
        var logDataFileDirectory = GetLogDataFileDirectory();
        if (!Directory.Exists(logDataFileDirectory)) Directory.CreateDirectory(logDataFileDirectory);

        var logDataFilePath = GetLogDataFilePath();
        if (!File.Exists(logDataFilePath)) return new List<JObject>();

        var raw = File.ReadAllText(logDataFilePath);
        if (string.IsNullOrWhiteSpace(raw)) return new List<JObject>();

        try
        {
            var jArray = JArray.Parse(raw);
            return jArray.Children<JObject>().ToList();
        }
        catch
        {
            // Attempt to read single object and wrap
            try
            {
                var single = JObject.Parse(raw);
                if (single != null) return new List<JObject> { single };
            }
            catch { }
            throw;
        }
    }
    
    public string ToJsonString()
    {
        var jArray = new JArray(_logEntries);
        return jArray.ToString(Newtonsoft.Json.Formatting.Indented);
    }
}