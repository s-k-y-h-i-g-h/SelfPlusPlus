using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;


namespace SelfPlusPlusCLI.Common;

public class LogDataService
{
    public const string LogDataFileName = "LogData.json";
    
    private readonly ILogger _logger;
    private List<JObject> _logEntries = new();


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
        if (!Directory.Exists(logDataFileDirectory))
        {
            Directory.CreateDirectory(logDataFileDirectory);
        }

        var logDataFilePath = GetLogDataFilePath();
        if (!File.Exists(logDataFilePath))
        {
            _logEntries = new List<JObject>();
            return _logEntries;
        }

        var raw = File.ReadAllText(logDataFilePath);
        if (string.IsNullOrWhiteSpace(raw))
        {
            _logEntries = new List<JObject>();
            return _logEntries;
        }

        try
        {
            var jArray = JArray.Parse(raw);
            _logEntries = jArray.Children<JObject>().ToList();
            return _logEntries;
        }
        catch
        {
            // Attempt to read single object and wrap
            try
            {
                var single = JObject.Parse(raw);
                if (single != null)
                {
                    _logEntries = new List<JObject> { single };
                    return _logEntries;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse log data file at {LogDataFilePath}", logDataFilePath);
                throw;
            }

            throw;
        }
    }

    public void AddLogEntry(JObject entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        try
        {
            ReadLogEntries();
            _logEntries.Add(entry);
            WriteLogEntries(_logEntries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add log entry");
            throw;
        }
    }

    private void WriteLogEntries(List<JObject> entries)
    {
        var logDataFileDirectory = GetLogDataFileDirectory();
        if (!Directory.Exists(logDataFileDirectory))
        {
            Directory.CreateDirectory(logDataFileDirectory);
        }

        var logDataFilePath = GetLogDataFilePath();
        var jArray = new JArray(entries);
        File.WriteAllText(logDataFilePath, jArray.ToString(Newtonsoft.Json.Formatting.Indented));
        _logEntries = entries;
    }
    
    public string ToJsonString()
    {
        var entries = ReadLogEntries();
        var jArray = new JArray(entries);
        return jArray.ToString(Newtonsoft.Json.Formatting.Indented);
    }
}