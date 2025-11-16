using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json.Linq;
using SelfPlusPlusCLI.Add;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Import;

class SamsungHealthImporter
{
    private const string SleepSummaryPattern = "com.samsung.shealth.sleep*.csv";
    private const string SleepStagePattern = "com.samsung.health.sleep_stage*.csv";
    private static readonly string[] DateTimeFormats =
    [
        "yyyy-MM-dd HH:mm:ss.fff",
        "yyyy-MM-dd HH:mm:ss"
    ];

    public SamsungHealthImportResult Import(string? directory, LogDataService logDataService)
    {
        ArgumentNullException.ThrowIfNull(logDataService);

        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("Samsung Health export directory is required.", nameof(directory));
        }

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Samsung Health export directory '{directory}' was not found.");
        }

        var existingEntries = logDataService.ReadLogEntries();
        var existingSleepSessionKeys = BuildExistingSleepSessionKeys(existingEntries);
        var existingSleepNoteKeys = BuildExistingSleepNoteKeys(existingEntries);

        var sleepSummaryPath = FindLatestFile(directory, SleepSummaryPattern, info =>
            info.Name.Contains(".sleep.", StringComparison.OrdinalIgnoreCase));
        if (sleepSummaryPath is null)
        {
            throw new FileNotFoundException($"No Samsung Health sleep summary file matching '{SleepSummaryPattern}' was found in '{directory}'.");
        }

        var sessions = LoadSleepSessions(sleepSummaryPath);
        if (sessions.Count == 0)
        {
            return SamsungHealthImportResult.Empty;
        }

        var stagePath = FindLatestFile(directory, SleepStagePattern, info =>
            info.Name.Contains(".sleep_stage.", StringComparison.OrdinalIgnoreCase));
        var stageDurationsBySleepId = stagePath is not null
            ? LoadStageDurations(stagePath)
            : new Dictionary<string, StageDurations>(StringComparer.OrdinalIgnoreCase);

        var sessionsProcessed = 0;
        var sessionsAdded = 0;
        var notesAdded = 0;
        var entriesToPersist = new List<JObject>();

        foreach (var session in sessions)
        {
            var start = ParseSamsungDateTime(session.StartTimeRaw, session.TimeOffset);
            var end = ParseSamsungDateTime(session.EndTimeRaw, session.TimeOffset);
            var durationMinutes = session.SleepDurationMinutes ?? CalculateDurationMinutes(start, end);

            var effectiveTimestamp = end ?? start ?? ParseSamsungDateTime(session.UpdateTimeRaw, session.TimeOffset) ??
                                     ParseSamsungDateTime(session.CreateTimeRaw, session.TimeOffset) ??
                                     DateTimeOffset.UtcNow;

            var stageDurations = session.DataUuid is not null && stageDurationsBySleepId.TryGetValue(session.DataUuid, out var value)
                ? value
                : null;

            var sleepEntry = BuildSleepLogEntry(session, stageDurations, effectiveTimestamp, durationMinutes);

            var key = BuildSleepSessionKey(session.DataUuid, sleepEntry.Timestamp, sleepEntry.DurationMinutes);
            var isNewSleepEntry = existingSleepSessionKeys.Add(key);

            if (isNewSleepEntry)
            {
                entriesToPersist.Add(JObject.FromObject(sleepEntry));
                sessionsAdded++;
            }

            // Create a note entry with the start time if available and it doesn't already exist
            if (start.HasValue)
            {
                var startTimestamp = NormalizeTimestamp(start.Value);
                var noteKey = BuildSleepNoteKey(startTimestamp);
                if (existingSleepNoteKeys.Add(noteKey))
                {
                    var noteEntry = new NoteLogEntry
                    {
                        Timestamp = startTimestamp,
                        Category = "Sleep",
                        Content = startTimestamp
                    };
                    entriesToPersist.Add(JObject.FromObject(noteEntry));
                    notesAdded++;
                }
            }

            sessionsProcessed++;
        }

        if (entriesToPersist.Count > 0)
        {
            logDataService.AddLogEntries(entriesToPersist);
        }

        return new SamsungHealthImportResult(sessionsProcessed, sessionsAdded, notesAdded);
    }

    private static HashSet<string> BuildExistingSleepSessionKeys(IEnumerable<JObject> entries)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var type = entry["Type"]?.ToString();
            var category = entry["Category"]?.ToString();

            if (!string.Equals(type, "Measurement", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(category, "Sleep", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var timestamp = NormalizeTimestamp(entry["Timestamp"]);
            var duration = entry["DurationMinutes"]?.ToObject<double?>();

            keys.Add(BuildSleepSessionKey(null, timestamp, duration));
        }

        return keys;
    }

    private static HashSet<string> BuildExistingSleepNoteKeys(IEnumerable<JObject> entries)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var type = entry["Type"]?.ToString();
            var category = entry["Category"]?.ToString();

            if (!string.Equals(type, "Note", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(category, "Sleep", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var timestamp = NormalizeTimestamp(entry["Timestamp"]);
            keys.Add(BuildSleepNoteKey(timestamp));
        }

        return keys;
    }

    private static string BuildSleepSessionKey(string? sourceId, string timestamp, double? durationMinutes)
    {
        var timestampFragment = string.IsNullOrWhiteSpace(timestamp) ? "timestamp:unknown" : $"timestamp:{timestamp}";
        var durationFragment = durationMinutes.HasValue
            ? $"duration:{durationMinutes.Value.ToString("0.###", CultureInfo.InvariantCulture)}"
            : "duration:unknown";

        return $"{timestampFragment}|{durationFragment}";
    }

    private static string BuildSleepNoteKey(string timestamp)
    {
        return string.IsNullOrWhiteSpace(timestamp) ? "timestamp:unknown" : $"timestamp:{timestamp}";
    }

    private static SleepLogEntry BuildSleepLogEntry(
        SamsungSleepSession session,
        StageDurations? stageDurations,
        DateTimeOffset effectiveTimestamp,
        double? durationMinutes)
    {
        var entry = new SleepLogEntry
        {
            Timestamp = NormalizeTimestamp(effectiveTimestamp),
            DurationMinutes = RoundValue(durationMinutes),
            Score = RoundValue(session.SleepScore),
            WakeScore = RoundValue(session.WakeScore),
            Efficiency = RoundValue(session.Efficiency),
            MentalRecovery = RoundValue(session.MentalRecovery),
            PhysicalRecovery = RoundValue(session.PhysicalRecovery)
        };

        if (stageDurations is not null && stageDurations.HasData)
        {
            entry.AwakeDurationMinutes = RoundValue(stageDurations.AwakeMinutes);
            entry.LightDurationMinutes = RoundValue(stageDurations.LightMinutes);
            entry.REMDurationMinutes = RoundValue(stageDurations.RemMinutes);
            entry.DeepDurationMinutes = RoundValue(stageDurations.DeepMinutes);
        }
        else
        {
            entry.LightDurationMinutes = RoundValue(session.TotalLightDurationMinutes);
            entry.REMDurationMinutes = RoundValue(session.TotalRemDurationMinutes);
        }

        return entry;
    }


    private static double? RoundValue(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
        {
            return null;
        }

        return Math.Round(value.Value, 2, MidpointRounding.AwayFromZero);
    }

    private static List<SamsungSleepSession> LoadSleepSessions(string filePath)
    {
        using var reader = OpenCsvReader(filePath, "com.samsung.shealth.sleep");
        using var csv = new CsvReader(reader, CreateCsvConfiguration());
        csv.Context.RegisterClassMap<SamsungSleepSessionMap>();

        var records = new List<SamsungSleepSession>();
        while (csv.Read())
        {
            SamsungSleepSession record;
            try
            {
                record = csv.GetRecord<SamsungSleepSession>();
            }
            catch (CsvHelperException)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(record.DataUuid))
            {
                continue;
            }

            var hasMeaningfulValue =
                record.SleepScore.HasValue ||
                record.Efficiency.HasValue ||
                record.SleepDurationMinutes.HasValue ||
                record.TotalLightDurationMinutes.HasValue ||
                record.TotalRemDurationMinutes.HasValue ||
                record.MentalRecovery.HasValue ||
                record.PhysicalRecovery.HasValue;

            if (!hasMeaningfulValue)
            {
                continue;
            }

            records.Add(record);
        }

        return records;
    }

    private static Dictionary<string, StageDurations> LoadStageDurations(string filePath)
    {
        using var reader = OpenCsvReader(filePath, "com.samsung.health.sleep_stage");
        using var csv = new CsvReader(reader, CreateCsvConfiguration());
        csv.Context.RegisterClassMap<SamsungSleepStageRecordMap>();

        var durations = new Dictionary<string, StageDurations>(StringComparer.OrdinalIgnoreCase);

        while (csv.Read())
        {
            SamsungSleepStageRecord record;
            try
            {
                record = csv.GetRecord<SamsungSleepStageRecord>();
            }
            catch (CsvHelperException)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(record.SleepId))
            {
                continue;
            }

            if (!int.TryParse(record.StageCode, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stageCode))
            {
                continue;
            }

            var start = ParseSamsungDateTime(record.StartTimeRaw, record.TimeOffset);
            var end = ParseSamsungDateTime(record.EndTimeRaw, record.TimeOffset);

            if (!start.HasValue || !end.HasValue)
            {
                continue;
            }

            var minutes = (end.Value - start.Value).TotalMinutes;
            if (minutes <= 0)
            {
                continue;
            }

            if (!durations.TryGetValue(record.SleepId, out var stageDurations))
            {
                stageDurations = new StageDurations();
                durations[record.SleepId] = stageDurations;
            }

            stageDurations.AddDuration(stageCode, minutes);
        }

        return durations;
    }

    private static double? CalculateDurationMinutes(DateTimeOffset? start, DateTimeOffset? end)
    {
        if (!start.HasValue || !end.HasValue)
        {
            return null;
        }

        var minutes = (end.Value - start.Value).TotalMinutes;
        return minutes > 0 ? minutes : null;
    }

    private static string? BuildDurationDescription(DateTimeOffset? start, DateTimeOffset? end)
    {
        if (!start.HasValue && !end.HasValue)
        {
            return null;
        }

        var culture = CultureInfo.CurrentCulture;
        var startText = start.HasValue ? start.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", culture) : "Unknown";
        var endText = end.HasValue ? end.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", culture) : "Unknown";

        return $"Start: {startText}; End: {endText}";
    }

    private static CsvConfiguration CreateCsvConfiguration()
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null,
            MissingFieldFound = null,
            HeaderValidated = null,
            IgnoreBlankLines = true
        };
    }

    private static TextReader OpenCsvReader(string filePath, string metadataPrefix)
    {
        var stream = File.OpenRead(filePath);
        var reader = new StreamReader(stream);

        if (reader.EndOfStream)
        {
            return reader;
        }

        var firstLine = reader.ReadLine();
        if (firstLine is null)
        {
            return reader;
        }

        if (!firstLine.StartsWith(metadataPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var remaining = reader.ReadToEnd();
            reader.Dispose();
            stream.Dispose();

            var builder = new System.Text.StringBuilder();
            builder.AppendLine(firstLine);
            builder.Append(remaining);
            return new StringReader(builder.ToString());
        }

        return reader;
    }

    private static DateTimeOffset? ParseSamsungDateTime(string? value, string? offsetString)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DateTime.TryParseExact(value, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return null;
        }

        var offset = ParseOffset(offsetString) ?? TimeSpan.Zero;
        return new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified), offset);
    }

    private static TimeSpan? ParseOffset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!value.StartsWith("UTC", StringComparison.OrdinalIgnoreCase) || value.Length < 7)
        {
            return null;
        }

        var sign = value[3];
        if (sign != '+' && sign != '-')
        {
            return null;
        }

        if (!int.TryParse(value.AsSpan(4, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours))
        {
            return null;
        }

        if (!int.TryParse(value.AsSpan(6, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
        {
            return null;
        }

        var offset = new TimeSpan(hours, minutes, 0);
        return sign == '-' ? offset.Negate() : offset;
    }

    private static string NormalizeTimestamp(JToken? token)
    {
        if (token is null || token.Type == JTokenType.Null)
        {
            return string.Empty;
        }

        if (token.Type == JTokenType.Date && token is JValue dateValue)
        {
            if (dateValue.Value is DateTimeOffset dto)
            {
                return NormalizeTimestamp(dto);
            }

            if (dateValue.Value is DateTime dt)
            {
                if (dt.Kind == DateTimeKind.Unspecified)
                {
                    dt = DateTime.SpecifyKind(dt, DateTimeKind.Local);
                }

                return NormalizeTimestamp(new DateTimeOffset(dt));
            }
        }

        return NormalizeTimestamp(token.ToString());
    }

    private static string NormalizeTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto) ||
            DateTimeOffset.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out dto))
        {
            return NormalizeTimestamp(dto);
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt) ||
            DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dt))
        {
            return NormalizeTimestamp(new DateTimeOffset(dt));
        }

        return value.Trim();
    }

    private static string NormalizeTimestamp(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
    }

    private static string? FindLatestFile(string directory, string pattern, Func<FileInfo, bool>? predicate = null)
    {
        var files = Directory
            .GetFiles(directory, pattern, SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .Where(info => predicate?.Invoke(info) ?? true)
            .ToList();

        if (files.Count == 0)
        {
            return null;
        }

        return files
            .OrderByDescending(info => info.LastWriteTimeUtc)
            .ThenByDescending(info => info.FullName, StringComparer.OrdinalIgnoreCase)
            .First()
            .FullName;
    }

    private sealed class SamsungSleepSession
    {
        public string? DataUuid { get; set; }
        public string? TimeOffset { get; set; }
        public string? StartTimeRaw { get; set; }
        public string? EndTimeRaw { get; set; }
        public string? UpdateTimeRaw { get; set; }
        public string? CreateTimeRaw { get; set; }
        public double? SleepScore { get; set; }
        public double? Efficiency { get; set; }
        public double? SleepDurationMinutes { get; set; }
        public double? TotalLightDurationMinutes { get; set; }
        public double? TotalRemDurationMinutes { get; set; }
        public double? WakeScore { get; set; }
        public double? PhysicalRecovery { get; set; }
        public double? MentalRecovery { get; set; }
        public double? Quality { get; set; }
    }

    private sealed class SamsungSleepSessionMap : ClassMap<SamsungSleepSession>
    {
        public SamsungSleepSessionMap()
        {
            Map(m => m.DataUuid).Name("com.samsung.health.sleep.datauuid");
            Map(m => m.TimeOffset).Name("com.samsung.health.sleep.time_offset");
            Map(m => m.StartTimeRaw).Name("com.samsung.health.sleep.start_time");
            Map(m => m.EndTimeRaw).Name("com.samsung.health.sleep.end_time");
            Map(m => m.UpdateTimeRaw).Name("com.samsung.health.sleep.update_time");
            Map(m => m.CreateTimeRaw).Name("com.samsung.health.sleep.create_time");
            Map(m => m.SleepScore).Name("sleep_score");
            Map(m => m.Efficiency).Name("efficiency");
            Map(m => m.SleepDurationMinutes).Name("sleep_duration");
            Map(m => m.TotalLightDurationMinutes).Name("total_light_duration");
            Map(m => m.TotalRemDurationMinutes).Name("total_rem_duration");
            Map(m => m.WakeScore).Name("wake_score");
            Map(m => m.PhysicalRecovery).Name("physical_recovery");
            Map(m => m.MentalRecovery).Name("mental_recovery");
            Map(m => m.Quality).Name("quality");
        }
    }

    private sealed class SamsungSleepStageRecord
    {
        public string? SleepId { get; set; }
        public string? StageCode { get; set; }
        public string? StartTimeRaw { get; set; }
        public string? EndTimeRaw { get; set; }
        public string? TimeOffset { get; set; }
    }

    private sealed class SamsungSleepStageRecordMap : ClassMap<SamsungSleepStageRecord>
    {
        public SamsungSleepStageRecordMap()
        {
            Map(m => m.SleepId).Name("sleep_id");
            Map(m => m.StageCode).Name("stage");
            Map(m => m.StartTimeRaw).Name("start_time");
            Map(m => m.EndTimeRaw).Name("end_time");
            Map(m => m.TimeOffset).Name("time_offset");
        }
    }

    private sealed class StageDurations
    {
        public double AwakeMinutes { get; private set; }
        public double LightMinutes { get; private set; }
        public double RemMinutes { get; private set; }
        public double DeepMinutes { get; private set; }
        public double UnmappedMinutes { get; private set; }

        public bool HasData =>
            AwakeMinutes > 0 ||
            LightMinutes > 0 ||
            RemMinutes > 0 ||
            DeepMinutes > 0 ||
            UnmappedMinutes > 0;

        public void AddDuration(int stageCode, double minutes)
        {
            switch (stageCode)
            {
                case 40001:
                    AwakeMinutes += minutes;
                    break;
                case 40002:
                    LightMinutes += minutes;
                    break;
                case 40003:
                    RemMinutes += minutes;
                    break;
                case 40004:
                    DeepMinutes += minutes;
                    break;
                default:
                    UnmappedMinutes += minutes;
                    break;
            }
        }
    }
}

internal sealed record SamsungHealthImportResult(int SessionsProcessed, int MeasurementsAdded, int NotesAdded)
{
    public static SamsungHealthImportResult Empty { get; } = new(0, 0, 0);
}