using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;


namespace SelfPlusPlusCLI.Common;

public class LogDataService
{
    private readonly ILogger _logger;
    private readonly ILogDataPathProvider _pathProvider;
    private List<JObject> _logEntries = new();

    public LogDataService(ILogger<LogDataService> logger, ILogDataPathProvider pathProvider)
    {
        _logger = logger;
        _pathProvider = pathProvider;

        // open data file
        _logEntries = ReadLogEntries();
    }

    public string GetLogDataFileDirectory()
    {
        return _pathProvider.GetLogDataDirectory();
    }
    
    public string GetLogDataFilePath()
    {
        return _pathProvider.GetLogDataFilePath();
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