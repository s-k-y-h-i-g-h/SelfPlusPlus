using System.Globalization;
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
            AddLogEntries(new[] { entry }, sortByTimestamp: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add log entry");
            throw;
        }
    }

    public void AddLogEntries(IEnumerable<JObject> entriesToAdd, bool sortByTimestamp = true)
    {
        if (entriesToAdd == null) throw new ArgumentNullException(nameof(entriesToAdd));

        try
        {
            var additions = entriesToAdd
                .Where(entry => entry is not null)
                .ToList();

            if (additions.Count == 0)
            {
                if (sortByTimestamp)
                {
                    SortExistingEntries();
                }

                return;
            }

            var combinedEntries = ReadLogEntries();
            combinedEntries.AddRange(additions);

            if (sortByTimestamp)
            {
                combinedEntries = SortEntriesByTimestamp(combinedEntries);
            }

            WriteLogEntries(combinedEntries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add log entries");
            throw;
        }
    }

    internal void WriteLogEntries(List<JObject> entries)
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

    public void SortExistingEntries()
    {
        try
        {
            var entries = ReadLogEntries();
            if (entries.Count <= 1)
            {
                return;
            }

            var sorted = SortEntriesByTimestamp(entries);
            WriteLogEntries(sorted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sort log entries");
            throw;
        }
    }

    private static List<JObject> SortEntriesByTimestamp(IEnumerable<JObject> entries)
    {
        return entries
            .OrderBy(entry => BuildSortKey(entry))
            .ToList();
    }

    private static (int Priority, DateTimeOffset Timestamp, string RawTimestamp, string Type, string Category, string Name) BuildSortKey(JObject entry)
    {
        var rawTimestamp = entry["Timestamp"]?.ToString() ?? string.Empty;
        var parsedTimestamp = TryParseTimestamp(rawTimestamp);
        var type = entry["Type"]?.ToString() ?? string.Empty;
        var category = entry["Category"]?.ToString() ?? string.Empty;
        var name = entry["Name"]?.ToString() ?? string.Empty;

        return (
            parsedTimestamp.HasValue ? 0 : 1,
            parsedTimestamp ?? DateTimeOffset.MinValue,
            rawTimestamp,
            type,
            category,
            name);
    }

    private static DateTimeOffset? TryParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto) ||
            DateTimeOffset.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out dto))
        {
            return dto;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt) ||
            DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dt))
        {
            if (dt.Kind == DateTimeKind.Unspecified)
            {
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Local);
            }

            return new DateTimeOffset(dt);
        }

        return null;
    }
}