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
                    case "Add":
                        HandleAdd(store, options);
                        break;
                    case "Update":
                        HandleUpdate(store, options);
                        break;
                    case "Remove":
                        HandleRemove(store, options);
                        break;
                    default:
                        Console.Error.WriteLine($"Error: Unsupported Action '{options.Action}'.");
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
                Type = options.Type,
                Category = options.Category,
                Name = options.Name,
                Amount = options.Amount,
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
                throw new InvalidOperationException("-Timestamp is required for Action 'Update'.");
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
                Amount = existing.Amount,
                Value = existing.Value,
                Unit = existing.Unit
            };

            if (options.HasType) fields.Type = options.Type;
            if (options.HasCategory) fields.Category = options.Category;
            if (options.HasName) fields.Name = options.Name;
            if (options.HasAmount) fields.Amount = options.Amount;
            if (options.HasValue) fields.Value = options.Value;
            if (options.HasUnit) fields.Unit = options.Unit;

            var updated = EntryFactory.CreateValidated(fields);
            entries[idx] = updated;
            store.WriteEntries(entries);
        }

        private static void HandleRemove(LogStore store, CliOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Timestamp))
            {
                throw new InvalidOperationException("-Timestamp is required for Action 'Remove'.");
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
    }

    internal sealed class CliOptions
    {
        public string Action { get; private set; } = string.Empty;
        public string? Type { get; private set; }
        public string? Category { get; private set; }
        public string? Name { get; private set; }
        public string? Amount { get; private set; }
        public double? Value { get; private set; }
        public string? Unit { get; private set; }
        public string? Timestamp { get; private set; }

        public bool HasType { get; private set; }
        public bool HasCategory { get; private set; }
        public bool HasName { get; private set; }
        public bool HasAmount { get; private set; }
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

                string? Next()
                {
                    if (i + 1 < args.Length) { return args[++i]; }
                    return null;
                }

                switch (a)
                {
                    case "Action":
                        opts.Action = Next() ?? string.Empty;
                        break;
                    case "Type":
                        opts.Type = Next(); opts.HasType = true; break;
                    case "Category":
                        opts.Category = Next(); opts.HasCategory = true; break;
                    case "Name":
                        opts.Name = Next(); opts.HasName = true; break;
                    case "Amount":
                        opts.Amount = Next(); opts.HasAmount = true; break;
                    case "Value":
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
                    case "Unit":
                        opts.Unit = Next(); opts.HasUnit = true; break;
                    case "Timestamp":
                        opts.Timestamp = Next(); break;
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
            Console.WriteLine("  Log --Action Add --Type <Consumption|Measurement> --Category <...> --Name <Name> [other params]");
            Console.WriteLine("  Log --Action Update --Timestamp <ISO8601 or local> [fields to change]");
            Console.WriteLine("  Log --Action Remove --Timestamp <ISO8601 or local>");
            Console.WriteLine();
            Console.WriteLine("Parameters:");
            Console.WriteLine("  --Action     Add | Update | Remove");
            Console.WriteLine("  --Type       Consumption | Measurement (required for Add, optional for Update)");
            Console.WriteLine("  --Category   For Consumption: Substance | Stack. For Measurement: Vitals");
            Console.WriteLine("  --Name       Entry name (required for Add, optional for Update)");
            Console.WriteLine("  --Amount     String amount (required for Consumption:Substance)");
            Console.WriteLine("  --Value      Float value (required for Measurement)");
            Console.WriteLine("  --Unit       Unit string (required for Measurement)");
            Console.WriteLine("  --Timestamp  Optional for Add; if given, used as event time. Required for Update/Remove");
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
        public string? Amount { get; set; }
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

            var type = fields.Type;
            var category = fields.Category;

            switch (type)
            {
                case "Consumption":
                    if (category != "Substance" && category != "Stack")
                        throw new InvalidOperationException("For Type 'Consumption', Category must be 'Substance' or 'Stack'.");
                    if (category == "Substance" && string.IsNullOrWhiteSpace(fields.Amount))
                        throw new InvalidOperationException("Amount is required when Type='Consumption' and Category='Substance'.");
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
                Type = fields.Type!,
                Category = fields.Category!,
                Name = fields.Name!,
                Amount = string.IsNullOrWhiteSpace(fields.Amount) ? null : fields.Amount,
                Value = fields.Value,
                Unit = string.IsNullOrWhiteSpace(fields.Unit) ? null : fields.Unit
            };
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

        [JsonPropertyName("Amount")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Amount { get; set; }

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
                var list = JsonSerializer.Deserialize<List<LogEntry>>(raw, JsonOptions);
                return list ?? new List<LogEntry>();
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


