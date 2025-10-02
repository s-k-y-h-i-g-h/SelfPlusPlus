using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SelfPlusPlus.LogApp
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                var options = CliOptions.Parse(args);
                if (options == null)
                {
                    CliOptions.PrintUsage();
                    return 1;
                }

                var store = new LogStore();

                switch (options.Action)
                {
                    case "add":
                        HandleAdd(store, options);
                        break;
                    case "update":
                        HandleUpdate(store, options);
                        break;
                    case "remove":
                        HandleRemove(store, options);
                        break;
                    case "show":
                        HandleShow(store, options);
                        break;
                    default:
                        Console.Error.WriteLine($"Error: unsupported action '{options.Action}'.");
                        CliOptions.PrintUsage();
                        return 1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static void HandleAdd(LogStore store, CliOptions options)
        {
            // Timestamp: use provided or generate now in UTC ISO 8601
            var timestampIso = TimestampHelpers.FormatTimestamp(options.Timestamp, generateNowIfMissing: true);

            var baseFields = new EntryFields
            {
                Timestamp = timestampIso,
                Type = Canonicalize.Type(options.Type),
                Category = Canonicalize.Category(options.Type, options.Category),
                Name = options.Name,
                Value = options.Value,
                Unit = options.Unit
            };

            var validated = EntryFactory.CreateValidated(baseFields);

            var entries = store.ReadEntries();
            entries.Add(validated);
            store.WriteEntries(entries);
        }

        private static void HandleUpdate(LogStore store, CliOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Timestamp))
            {
                throw new InvalidOperationException("--timestamp is required for action 'update'.");
            }

            var entries = store.ReadEntries();
            if (entries.Count == 0)
            {
                throw new InvalidOperationException("No entries found to update.");
            }

            var idx = EntryMatcher.FindIndexByTimestamp(entries, options.Timestamp);
            if (idx < 0)
            {
                throw new InvalidOperationException("No entry found with the specified Timestamp.");
            }

            var existing = entries[idx];

            var fields = new EntryFields
            {
                Timestamp = existing.Timestamp,
                Type = existing.Type,
                Category = existing.Category,
                Name = existing.Name,
                Value = existing.Value,
                Unit = existing.Unit
            };

            if (options.HasType) fields.Type = Canonicalize.Type(options.Type);
            if (options.HasCategory) fields.Category = Canonicalize.Category(options.HasType ? options.Type : existing.Type, options.Category);
            if (options.HasName) fields.Name = options.Name;
            if (options.HasValue) fields.Value = options.Value;
            if (options.HasUnit) fields.Unit = options.Unit;

            // Require at least one of the Add-required fields to be provided
            if (!options.HasType && !options.HasCategory && !options.HasName)
            {
                throw new InvalidOperationException("at least one of --type, --category, or --name must be provided for 'update'.");
            }

            var updated = EntryFactory.CreateValidated(fields);
            entries[idx] = updated;
            store.WriteEntries(entries);
        }

        private static void HandleRemove(LogStore store, CliOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Timestamp))
            {
                throw new InvalidOperationException("--timestamp is required for action 'remove'.");
            }

            var entries = store.ReadEntries();
            if (entries.Count == 0)
            {
                throw new InvalidOperationException("No entries found to remove.");
            }

            var idx = EntryMatcher.FindIndexByTimestamp(entries, options.Timestamp);
            if (idx < 0)
            {
                throw new InvalidOperationException("No entry found with the specified Timestamp.");
            }

            entries.RemoveAt(idx);
            store.WriteEntries(entries);
        }

        private static void HandleShow(LogStore store, CliOptions options)
        {
            var hasTimestamp = !string.IsNullOrWhiteSpace(options.Timestamp);
            var hasDate = !string.IsNullOrWhiteSpace(options.Date);
            var hasRange = !string.IsNullOrWhiteSpace(options.Range);

            var provided = (hasTimestamp ? 1 : 0) + (hasDate ? 1 : 0) + (hasRange ? 1 : 0);
            if (provided > 1)
            {
                throw new InvalidOperationException("specify only one of --timestamp, --date, or --range for action 'show'.");
            }

            var entries = store.ReadEntries();

            if (hasTimestamp)
            {
                var ts = options.Timestamp!;
                if (IsRoundtripIso(ts))
                {
                    var match = entries.FirstOrDefault(e => e.Timestamp == ts);
                    if (match == null)
                    {
                        Console.WriteLine("No entries found for the specified criteria.");
                        return;
                    }
                    PrintEntry(match);
                    return;
                }

                var parsed = TimestampHelpers.TryParseFlexible(ts);
                if (!parsed.HasValue)
                {
                    Console.WriteLine("No entries found for the specified criteria.");
                    return;
                }

                var targetTicks = parsed.Value.ToUniversalTime().UtcTicks;
                foreach (var e in entries)
                {
                    var entryKey = TimestampHelpers.GetUtcTicksKey(e.Timestamp);
                    if (entryKey.HasValue && entryKey.Value == targetTicks)
                    {
                        PrintEntry(e);
                        return;
                    }
                }

                Console.WriteLine("No entries found for the specified criteria.");
                return;
            }

            var bounds = ResolveBounds(options);

            var filtered = new List<(LogEntry Entry, long UtcTicks)>();
            foreach (var e in entries)
            {
                var dto = TimestampHelpers.TryParseFlexible(e.Timestamp);
                if (!dto.HasValue) continue;
                var utc = dto.Value.ToUniversalTime();
                if (utc >= bounds.startUtc && utc < bounds.endExclusiveUtc)
                {
                    filtered.Add((e, utc.UtcTicks));
                }
            }

            if (filtered.Count == 0)
            {
                Console.WriteLine("No entries found for the specified criteria.");
                return;
            }

            foreach (var item in filtered.OrderBy(t => t.UtcTicks))
            {
                PrintEntry(item.Entry);
            }
        }

        private static (DateTimeOffset startUtc, DateTimeOffset endExclusiveUtc) ResolveBounds(CliOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.Range))
            {
                var range = options.Range!;
                if (!TryParseRangeDates(range, out var startDateLocal, out var endDateLocal))
                {
                    throw new InvalidOperationException("unable to parse --range boundaries");
                }

                var startUtc = ConvertLocalDateToUtcStart(startDateLocal);
                var endExclusiveUtc = ConvertLocalDateToUtcStart(endDateLocal.AddDays(1));
                return (startUtc, endExclusiveUtc);
            }

            if (!string.IsNullOrWhiteSpace(options.Date))
            {
                if (!TryParseDateOnly(options.Date!, out var localDate))
                {
                    // fallback to flexible parse if time provided
                    var parsed = TimestampHelpers.TryParseFlexible(options.Date);
                    if (!parsed.HasValue)
                    {
                        throw new InvalidOperationException("unable to parse --date value");
                    }
                    localDate = parsed.Value.ToLocalTime().Date;
                }
                var startUtc = ConvertLocalDateToUtcStart(localDate);
                var endExclusiveUtc = ConvertLocalDateToUtcStart(localDate.AddDays(1));
                return (startUtc, endExclusiveUtc);
            }

            // Default to today (local)
            var todayLocal = DateTime.Now.Date;
            var startTodayUtc = ConvertLocalDateToUtcStart(todayLocal);
            var endTodayExclusiveUtc = ConvertLocalDateToUtcStart(todayLocal.AddDays(1));
            return (startTodayUtc, endTodayExclusiveUtc);
        }

        private static bool TryParseRangeDates(string input, out DateTime startLocalDate, out DateTime endLocalDate)
        {
            startLocalDate = default;
            endLocalDate = default;

            if (string.IsNullOrWhiteSpace(input)) return false;

            var normalized = input.Replace('\u2013', '-') // en dash
                                  .Replace('\u2014', '-') // em dash
                                  .Trim();

            // Primary split on dash with optional spaces around it, but keep inner date hyphens intact
            var candidates = new List<(string a, string b)>();

            // Try a simple regex-like split: first '-' flanked by optional spaces
            int sepIndex = IndexOfRangeSeparator(normalized);
            if (sepIndex > 0 && sepIndex < normalized.Length - 1)
            {
                candidates.Add((normalized.Substring(0, sepIndex).Trim(), normalized.Substring(sepIndex + 1).Trim()));
            }

            // Fallback: try every '-' as a separator
            for (int i = 1; i < normalized.Length - 1; i++)
            {
                if (normalized[i] != '-') continue;
                var left = normalized.Substring(0, i).Trim();
                var right = normalized.Substring(i + 1).Trim();
                candidates.Add((left, right));
            }

            foreach (var (a, b) in candidates)
            {
                if (TryParseDateOnly(a, out var d1) && TryParseDateOnly(b, out var d2))
                {
                    // Ensure order; range is inclusive
                    if (d2 < d1)
                    {
                        (d1, d2) = (d2, d1);
                    }
                    startLocalDate = d1;
                    endLocalDate = d2;
                    return true;
                }
            }

            return false;
        }

        private static int IndexOfRangeSeparator(string s)
        {
            // Look for a '-' that has a date-like token on both sides when split
            for (int i = 1; i < s.Length - 1; i++)
            {
                if (s[i] != '-') continue;
                var left = s.Substring(0, i).Trim();
                var right = s.Substring(i + 1).Trim();
                if (TryParseDateOnly(left, out _) && TryParseDateOnly(right, out _))
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool TryParseDateOnly(string input, out DateTime localDate)
        {
            localDate = default;
            if (string.IsNullOrWhiteSpace(input)) return false;

            // Accept ISO yyyy-MM-dd
            if (DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var isoDate))
            {
                localDate = isoDate;
                return true;
            }

            // Accept dd/MM/yyyy regardless of current culture
            if (DateTime.TryParseExact(input, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dmy))
            {
                localDate = dmy;
                return true;
            }

            // Accept current culture date-only
            if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out var cultureDate))
            {
                localDate = cultureDate.Date;
                return true;
            }

            // As a last resort, let flexible parser try and then take Date
            var flex = TimestampHelpers.TryParseFlexible(input);
            if (flex.HasValue)
            {
                localDate = flex.Value.ToLocalTime().Date;
                return true;
            }

            return false;
        }

        private static DateTimeOffset ConvertLocalDateToUtcStart(DateTime localDate)
        {
            var localMidnightUnspecified = new DateTime(localDate.Year, localDate.Month, localDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localMidnightUnspecified, TimeZoneInfo.Local);
            return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
        }

        private static bool IsRoundtripIso(string s)
        {
            return DateTimeOffset.TryParseExact(s, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _);
        }

        private static void PrintEntry(LogEntry entry)
        {
            var dto = TimestampHelpers.TryParseFlexible(entry.Timestamp);
            string ts = dto.HasValue
                ? dto.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                : entry.Timestamp;

            var typeCategory = string.Concat(entry.Type, "/", entry.Category);
            var name = entry.Name;

            if (entry.Value.HasValue && !string.IsNullOrWhiteSpace(entry.Unit))
            {
                var val = entry.Value.Value.ToString(CultureInfo.InvariantCulture);
                Console.WriteLine(string.Concat(ts, "  ", typeCategory, "  ", name, "  ", val, " ", entry.Unit));
                return;
            }

            Console.WriteLine(string.Concat(ts, "  ", typeCategory, "  ", name));
        }
    }

    internal sealed class CliOptions
    {
        public string Action { get; private set; } = string.Empty;
        public string? Type { get; private set; }
        public string? Category { get; private set; }
        public string? Name { get; private set; }
        public double? Value { get; private set; }
        public string? Unit { get; private set; }
        public string? Timestamp { get; private set; }
        public string? Date { get; private set; }
        public string? Range { get; private set; }

        public bool HasType { get; private set; }
        public bool HasCategory { get; private set; }
        public bool HasName { get; private set; }
        public bool HasValue { get; private set; }
        public bool HasUnit { get; private set; }

        public static CliOptions? Parse(string[] args)
        {
            if (args.Length == 0)
            {
                return null;
            }

            var opts = new CliOptions();
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a.StartsWith("--")) a = a.Substring(2);
                else if (a.StartsWith("-")) a = a.Substring(1);
                a = a.ToLowerInvariant();

                string? Next()
                {
                    if (i + 1 < args.Length) { return args[++i]; }
                    return null;
                }

                switch (a)
                {
                    case "action":
                        opts.Action = (Next() ?? string.Empty).ToLowerInvariant();
                        break;
                    case "type":
                        opts.Type = Next(); opts.HasType = true; break;
                    case "category":
                        opts.Category = Next(); opts.HasCategory = true; break;
                    case "name":
                        opts.Name = Next(); opts.HasName = true; break;
                    case "value":
                        {
                            var s = Next();
                            if (!string.IsNullOrWhiteSpace(s) && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var dv))
                            {
                                opts.Value = dv; opts.HasValue = true;
                            }
                            else
                            {
                                // Still mark presence to allow validation to complain if needed
                                opts.HasValue = true;
                            }
                            break;
                        }
                    case "unit":
                        opts.Unit = Next(); opts.HasUnit = true; break;
                    case "timestamp":
                        opts.Timestamp = Next(); break;
                    case "date":
                        opts.Date = Next(); break;
                    case "range":
                        opts.Range = Next(); break;
                    default:
                        // ignore unknown tokens allowing simple future extension
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(opts.Action))
            {
                return null;
            }

            return opts;
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  log --action add --type <consumption|measurement> --category <...> --name <name> [other params]");
            Console.WriteLine("  log --action update --timestamp <ISO8601 or local> [fields to change]");
            Console.WriteLine("  log --action remove --timestamp <ISO8601 or local>");
            Console.WriteLine("  log --action show [--timestamp <ISO8601 or local>] [--date <date>] [--range \"start-end\"]");
            Console.WriteLine();
            Console.WriteLine("Parameters:");
            Console.WriteLine("  --action     add | update | remove | show");
            Console.WriteLine("  --type       consumption | measurement (required for add, optional for update)");
            Console.WriteLine("  --category   for consumption: substance | stack; for measurement: vitals");
            Console.WriteLine("  --name       entry name (required for add, optional for update)");
            Console.WriteLine("  --value      float value (required for consumption:substance and measurement)");
            Console.WriteLine("  --unit       unit string (required for consumption:substance and measurement)");
            Console.WriteLine("  --timestamp  optional for add; if given, used as event time. required for update/remove. for show: exact match if ISO8601 roundtrip; otherwise treated as local time");
            Console.WriteLine("  --date       for show: list entries on this local date");
            Console.WriteLine("  --range      for show: inclusive local date range, e.g. \"01/01/2024-01/01/2025\"");
            Console.WriteLine();
            Console.WriteLine("Notes:");
            Console.WriteLine("  show prints timestamps in your local time (no timezone offset)");
        }
    }

    internal static class TimestampHelpers
    {
        public static string? FormatTimestamp(string? s, bool generateNowIfMissing = false)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                if (generateNowIfMissing)
                {
                    return DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                }
                return null;
            }

            var parsed = TryParseFlexible(s);
            if (parsed.HasValue)
            {
                return parsed.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
            }

            return s; // leave as-is if we cannot parse
        }

        public static DateTimeOffset? TryParseFlexible(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;

            if (DateTimeOffset.TryParseExact(s, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dtoExact))
            {
                return dtoExact;
            }

            foreach (var culture in new[] { CultureInfo.InvariantCulture, CultureInfo.CurrentCulture })
            {
                foreach (var style in new[] { DateTimeStyles.AssumeUniversal, DateTimeStyles.AdjustToUniversal, DateTimeStyles.None })
                {
                    if (DateTimeOffset.TryParse(s, culture, style, out var dto))
                    {
                        return dto;
                    }
                }

                if (DateTime.TryParse(s, culture, DateTimeStyles.AssumeUniversal, out var dt))
                {
                    return new DateTimeOffset(dt);
                }
            }

            return null;
        }

        public static long? GetUtcTicksKey(string? s)
        {
            var parsed = TryParseFlexible(s);
            if (parsed.HasValue)
            {
                return parsed.Value.ToUniversalTime().UtcTicks;
            }

            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
            {
                return new DateTimeOffset(dt).UtcTicks;
            }

            return null;
        }
    }

    internal static class EntryMatcher
    {
        public static int FindIndexByTimestamp(List<LogEntry> entries, string timestampString)
        {
            if (entries == null || entries.Count == 0) return -1;

            var normalizedInput = TimestampHelpers.FormatTimestamp(timestampString);
            var inputKey = TimestampHelpers.GetUtcTicksKey(timestampString);

            for (int i = 0; i < entries.Count; i++)
            {
                var ts = entries[i].Timestamp;
                if (ts == timestampString) return i;

                var entryKey = TimestampHelpers.GetUtcTicksKey(ts);
                if (inputKey.HasValue && entryKey.HasValue && inputKey.Value == entryKey.Value) return i;

                var normalizedEntry = TimestampHelpers.FormatTimestamp(ts);
                if (!string.IsNullOrEmpty(normalizedEntry) && normalizedEntry == normalizedInput) return i;
            }

            return -1;
        }
    }

    internal sealed class EntryFields
    {
        public string? Timestamp { get; set; }
        public string? Type { get; set; }
        public string? Category { get; set; }
        public string? Name { get; set; }
        public double? Value { get; set; }
        public string? Unit { get; set; }
    }

    internal static class EntryFactory
    {
        public static LogEntry CreateValidated(EntryFields fields)
        {
            if (string.IsNullOrWhiteSpace(fields.Timestamp))
                throw new InvalidOperationException("Timestamp is required.");
            if (string.IsNullOrWhiteSpace(fields.Type))
                throw new InvalidOperationException("Type is required.");
            if (string.IsNullOrWhiteSpace(fields.Category))
                throw new InvalidOperationException("Category is required.");
            if (string.IsNullOrWhiteSpace(fields.Name))
                throw new InvalidOperationException("Name is required.");

            var type = Canonicalize.Type(fields.Type);
            var category = Canonicalize.Category(type, fields.Category);

            switch (type)
            {
                case "Consumption":
                    if (category != "Substance" && category != "Stack")
                        throw new InvalidOperationException("For Type 'Consumption', Category must be 'Substance' or 'Stack'.");
                    if (category == "Substance")
                    {
                        if (!fields.Value.HasValue)
                            throw new InvalidOperationException("Value (float) is required when Type='Consumption' and Category='Substance'.");
                        if (string.IsNullOrWhiteSpace(fields.Unit))
                            throw new InvalidOperationException("Unit is required when Type='Consumption' and Category='Substance'.");
                    }
                    break;
                case "Measurement":
                    if (category != "Vitals")
                        throw new InvalidOperationException("For Type 'Measurement', Category must be 'Vitals'.");
                    if (!fields.Value.HasValue)
                        throw new InvalidOperationException("Value (float) is required when Type='Measurement'.");
                    if (string.IsNullOrWhiteSpace(fields.Unit))
                        throw new InvalidOperationException("Unit is required when Type='Measurement'.");
                    break;
                default:
                    throw new InvalidOperationException("Unsupported Type '" + type + "'. Allowed: Consumption, Measurement.");
            }

            return new LogEntry
            {
                Timestamp = fields.Timestamp!,
                Type = type!,
                Category = category!,
                Name = fields.Name!,
                Value = fields.Value,
                Unit = string.IsNullOrWhiteSpace(fields.Unit) ? null : fields.Unit
            };
        }
    }

    internal static class Canonicalize
    {
        public static string? Type(string? type)
        {
            if (string.IsNullOrWhiteSpace(type)) return type;
            return type.Trim().ToLowerInvariant() switch
            {
                "consumption" => "Consumption",
                "measurement" => "Measurement",
                _ => type
            };
        }

        public static string? Category(string? type, string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) return category;
            var t = Type(type)?.ToLowerInvariant();
            var c = category.Trim().ToLowerInvariant();
            if (t == "consumption")
            {
                return c switch
                {
                    "substance" => "Substance",
                    "stack" => "Stack",
                    _ => category
                };
            }
            if (t == "measurement")
            {
                return c switch
                {
                    "vitals" => "Vitals",
                    _ => category
                };
            }
            return category;
        }
    }

    internal sealed class LogEntry
    {
        [JsonPropertyName("Timestamp")]
        public string Timestamp { get; set; } = string.Empty;
        [JsonPropertyName("Type")]
        public string Type { get; set; } = string.Empty;
        [JsonPropertyName("Category")]
        public string Category { get; set; } = string.Empty;
        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("Value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Value { get; set; }

        [JsonPropertyName("Unit")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Unit { get; set; }
    }

    internal sealed class LogStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        public List<LogEntry> ReadEntries()
        {
            var path = GetLogFilePath(ensureDirectory: true);
            if (!File.Exists(path)) return new List<LogEntry>();

            var raw = File.ReadAllText(path);
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

        public void WriteEntries(List<LogEntry> entries)
        {
            var path = GetLogFilePath(ensureDirectory: true);
            var json = JsonSerializer.Serialize(entries, JsonOptions);
            File.WriteAllText(path, json);
        }

        private static string GetLogFilePath(bool ensureDirectory)
        {
            string dir;
            if (OperatingSystem.IsWindows())
            {
                var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                dir = Path.Combine(basePath, "SelfPlusPlus");
            }
            else if (OperatingSystem.IsMacOS())
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                dir = Path.Combine(home, "Library", "Application Support", "SelfPlusPlus");
            }
            else
            {
                var xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                if (string.IsNullOrWhiteSpace(xdg))
                {
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    dir = Path.Combine(home, ".local", "share", "SelfPlusPlus");
                }
                else
                {
                    dir = Path.Combine(xdg, "SelfPlusPlus");
                }
            }

            if (ensureDirectory && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "Log.json");
        }
    }
}


