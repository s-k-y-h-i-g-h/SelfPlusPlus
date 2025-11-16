using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using SelfPlusPlusCLI.Common;
using Spectre.Console.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console.Rendering;

namespace SelfPlusPlusCLI.Show;

public class ShowCommand : Command<ShowSettings>
{
    private readonly IConfiguration _configuration;
    private readonly LogDataService _logDataService;
    private readonly IAnsiConsole _console;
    private const string SegmentSeparator = " [grey]•[/] ";

    public ShowCommand(IConfiguration configuration, LogDataService logDataService, IAnsiConsole console)
    {
        _configuration = configuration;
        _logDataService = logDataService;
        _console = console;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] ShowSettings settings)
    {
        if (settings.ShowPath)
        {
            var path = _logDataService.GetLogDataFilePath();
            _console.Write(path);
        } 
        else
        {
            var entries = _logDataService.ReadLogEntries();
            var timeZone = TimeZoneInfo.Local;

            bool TryParseDate(string input, out DateTime date)
            {
                if (DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var isoDate))
                {
                    date = isoDate;
                    return true;
                }

                if (DateTime.TryParseExact(input, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dmy))
                {
                    date = dmy;
                    return true;
                }

                if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out var cultureDate))
                {
                    date = cultureDate.Date;
                    return true;
                }

                date = default;
                return false;
            }

            bool TryParseTime(string input, out TimeSpan timeOfDay)
            {
                if (TimeSpan.TryParse(input, out var parsedTime))
                {
                    timeOfDay = parsedTime;
                    return true;
                }

                if (DateTime.TryParseExact(input, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeOnly))
                {
                    timeOfDay = timeOnly.TimeOfDay;
                    return true;
                }

                if (DateTime.TryParseExact(input, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeWithSeconds))
                {
                    timeOfDay = timeWithSeconds.TimeOfDay;
                    return true;
                }

                timeOfDay = default;
                return false;
            }

            DateTimeOffset ToLocalOffset(DateTime localDateTime)
            {
                return new DateTimeOffset(localDateTime, timeZone.GetUtcOffset(localDateTime));
            }

            DateTimeOffset? TryParseTimestamp(string? timestamp)
            {
                return ParseTimestamp(timestamp);
            }

            bool TryCreateBoundary(
                string? date,
                string? time,
                bool isEndBoundary,
                string dateOptionName,
                string timeOptionName,
                out DateTimeOffset? boundary,
                out string? errorMessage)
            {
                boundary = null;
                errorMessage = null;

                var hasDate = !string.IsNullOrWhiteSpace(date);
                var hasTime = !string.IsNullOrWhiteSpace(time);

                if (!hasDate && !hasTime)
                {
                    return true;
                }

                DateTime localDate;

                if (hasDate)
                {
                    if (!TryParseDate(date!, out localDate))
                    {
                        errorMessage = $"[red]Error: Unable to parse {dateOptionName}. Use format yyyy-MM-dd or dd/MM/yyyy.[/]";
                        return false;
                    }
                }
                else
                {
                    localDate = DateTime.Now.Date;
                }

                if (hasTime)
                {
                    if (!TryParseTime(time!, out var timeOfDay))
                    {
                        errorMessage = $"[red]Error: Unable to parse {timeOptionName}. Use format HH:mm or HH:mm:ss.[/]";
                        return false;
                    }

                    boundary = ToLocalOffset(localDate.Add(timeOfDay));
                    return true;
                }

                if (isEndBoundary && hasDate)
                {
                    boundary = ToLocalOffset(localDate.AddDays(1).AddTicks(-1));
                    return true;
                }

                boundary = ToLocalOffset(localDate);
                return true;
            }

            if (!TryCreateBoundary(settings.StartDate, settings.StartTime, false, "--start-date", "--start-time", out var startBoundary, out var startError))
            {
                _console.MarkupLine(startError!);
                return 1;
            }

            if (!TryCreateBoundary(settings.EndDate, settings.EndTime, true, "--end-date", "--end-time", out var endBoundary, out var endError))
            {
                _console.MarkupLine(endError!);
                return 1;
            }

            var hasStartBoundaryInput =
                !string.IsNullOrWhiteSpace(settings.StartDate) ||
                !string.IsNullOrWhiteSpace(settings.StartTime);
            var hasEndBoundaryInput =
                !string.IsNullOrWhiteSpace(settings.EndDate) ||
                !string.IsNullOrWhiteSpace(settings.EndTime);
            var hasBoundaryInput = hasStartBoundaryInput || hasEndBoundaryInput;

            if (!hasBoundaryInput && !settings.Total && !startBoundary.HasValue && !endBoundary.HasValue)
            {
                var today = DateTime.Now.Date;
                startBoundary = ToLocalOffset(today);
                endBoundary = ToLocalOffset(today.AddDays(1).AddTicks(-1));
            }

            if (settings.Total && !startBoundary.HasValue && !endBoundary.HasValue)
            {
                var today = DateTime.Now.Date;
                startBoundary = ToLocalOffset(today);
                endBoundary = ToLocalOffset(today.AddDays(1).AddTicks(-1));
            }

            var startUtc = startBoundary?.ToUniversalTime();
            var endUtc = endBoundary?.ToUniversalTime();

            if (startUtc.HasValue && endUtc.HasValue && startUtc > endUtc)
            {
                _console.MarkupLine("[red]Error: --start-date/time must be earlier than --end-date/time.[/]");
                return 1;
            }

            if (startUtc.HasValue || endUtc.HasValue)
            {
                var filteredEntries = new List<JObject>();

                foreach (var entry in entries)
                {
                    var timestampStr = entry["Timestamp"]?.ToString();
                    if (string.IsNullOrWhiteSpace(timestampStr))
                    {
                        continue;
                    }

                    var parsed = TryParseTimestamp(timestampStr);
                    if (!parsed.HasValue)
                    {
                        continue;
                    }

                    var entryUtc = parsed.Value.ToUniversalTime();

                    if (startUtc.HasValue && entryUtc < startUtc.Value)
                    {
                        continue;
                    }

                    if (endUtc.HasValue && entryUtc > endUtc.Value)
                    {
                        continue;
                    }

                    filteredEntries.Add(entry);
                }

                entries = filteredEntries;
            }

            entries = SortEntriesByTimestamp(entries);

            if (settings.Format == Format.JSON)
            {
                var jsonArray = new JArray(entries.Select(entry =>
                {
                    var clone = (JObject)entry.DeepClone();
                    var parsedTimestamp = ParseTimestamp(clone["Timestamp"]?.ToString());
                    if (parsedTimestamp.HasValue)
                    {
                        clone["Timestamp"] = FormatTimestamp(parsedTimestamp.Value);
                    }

                    return clone;
                }));
                var jsonText = new JsonText(jsonArray.ToString(Newtonsoft.Json.Formatting.Indented));
                _console.Write(jsonText);
                return 0;
            }

            if (!string.IsNullOrWhiteSpace(settings.EntryType))
            {
                entries = entries
                    .Where(entry => string.Equals(entry["Type"]?.ToString(), settings.EntryType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(settings.Category))
            {
                entries = entries
                    .Where(entry => string.Equals(entry["Category"]?.ToString(), settings.Category, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (settings.Total)
            {
                double? TryGetNumericValue(JToken? token)
                {
                    if (token == null || token.Type == JTokenType.Null)
                    {
                        return null;
                    }

                    if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
                    {
                        return token.Value<double>();
                    }

                    if (token.Type == JTokenType.String)
                    {
                        var raw = token.Value<string>();
                        if (double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var invariantResult))
                        {
                            return invariantResult;
                        }

                        if (double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var cultureResult))
                        {
                            return cultureResult;
                        }
                    }

                    return null;
                }

                var totals = new List<(DateTime LocalDate, string Category, string Name, string Unit, double Amount)>();

                foreach (var entry in entries)
                {
                    var type = entry["Type"]?.ToString();
                    if (!string.Equals(type, "Consumption", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var timestamp = TryParseTimestamp(entry["Timestamp"]?.ToString());
                    if (!timestamp.HasValue)
                    {
                        continue;
                    }

                    var amount = TryGetNumericValue(entry["Amount"]) ?? TryGetNumericValue(entry["Value"]);
                    if (!amount.HasValue)
                    {
                        continue;
                    }

                    var unit = entry["Unit"]?.ToString();
                    if (string.IsNullOrWhiteSpace(unit))
                    {
                        continue;
                    }

                    var category = entry["Category"]?.ToString() ?? string.Empty;
                    var name = entry["Name"]?.ToString() ?? string.Empty;
                    var localDate = timestamp.Value.ToLocalTime().Date;

                    totals.Add((localDate, category, name, unit, amount.Value));
                }

                var groupedTotals = totals
                    .GroupBy(t => new { t.LocalDate, Category = t.Category, Name = t.Name, Unit = t.Unit })
                    .Select(g => new
                    {
                        g.Key.LocalDate,
                        g.Key.Category,
                        g.Key.Name,
                        g.Key.Unit,
                        TotalAmount = g.Sum(x => x.Amount)
                    })
                    .OrderBy(x => x.LocalDate)
                    .ThenBy(x => x.Category, StringComparer.Ordinal)
                    .ThenBy(x => x.Name, StringComparer.Ordinal)
                    .ThenBy(x => x.Unit, StringComparer.Ordinal)
                    .ToList();

                if (groupedTotals.Count == 0)
                {
                    _console.WriteLine("No entries found for the specified criteria.");
                    return 0;
                }

                var totalsRows = groupedTotals
                    .Select(item => new
                    {
                        Date = item.LocalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        Type = "Consumption",
                        Category = item.Category ?? string.Empty,
                        Name = item.Name ?? string.Empty,
                        Amount = item.TotalAmount.ToString(CultureInfo.InvariantCulture),
                        Unit = item.Unit ?? string.Empty,
                        LocalDate = item.LocalDate
                    })
                    .OrderBy(row => row.LocalDate)
                    .ToList();

                var totalsTable = new Table();
                totalsTable.AddColumn("Date");
                totalsTable.AddColumn("Type");
                totalsTable.AddColumn("Category");
                totalsTable.AddColumn("Name");
                totalsTable.AddColumn("Amount");
                totalsTable.AddColumn("Unit");

                var dateColumnWidth = Math.Max("Date".Length, totalsRows.Max(r => r.Date.Length));
                var typeColumnWidth = Math.Max("Type".Length, totalsRows.Max(r => r.Type.Length));
                var categoryColumnWidth = Math.Max("Category".Length, totalsRows.Max(r => r.Category.Length));
                var nameColumnWidth = Math.Max("Name".Length, totalsRows.Max(r => r.Name.Length));
                var amountColumnWidth = Math.Max("Amount".Length, totalsRows.Max(r => r.Amount.Length));
                var unitColumnWidth = Math.Max("Unit".Length, totalsRows.Max(r => r.Unit.Length));

                DateTime? currentDate = null;

                IRenderable[] CreateSeparatorRow()
                {
                    return Enumerable.Range(0, totalsTable.Columns.Count)
                        .Select(index =>
                        {
                            var width = index switch
                            {
                                0 => dateColumnWidth,
                                1 => typeColumnWidth,
                                2 => categoryColumnWidth,
                                3 => nameColumnWidth,
                                4 => amountColumnWidth,
                                5 => unitColumnWidth,
                                _ => 0
                            };

                            var line = new string('─', Math.Max(width, 0));
                            return (IRenderable)new Markup($"{line}");
                        })
                        .ToArray();
                }

                foreach (var item in totalsRows)
                {
                    var dateText = item.Date;
                    var amountText = item.Amount;

                    if (currentDate.HasValue && item.LocalDate != currentDate.Value)
                    {
                        totalsTable.AddRow(CreateSeparatorRow());
                    }

                    totalsTable.AddRow(
                        Markup.Escape(dateText),
                        Markup.Escape(item.Type),
                        Markup.Escape(item.Category),
                        Markup.Escape(item.Name),
                        Markup.Escape(amountText),
                        Markup.Escape(item.Unit));
                    currentDate = item.LocalDate;
                }

                _console.Write(totalsTable);

                return 0;
            }

            var table = CreateEntryTable();

            foreach (var entry in entries)
            {
                var timestampMarkup = BuildTimestampMarkup(entry);
                var detailsMarkup = BuildDetailsMarkup(entry);

                table.AddRow(
                    new Markup(timestampMarkup),
                    new Markup(detailsMarkup));
            }

            _console.Write(table);
        }

        return 0;

    }

    private static Table CreateEntryTable()
    {
        var timestampColumn = new TableColumn(string.Empty)
        {
            Alignment = Justify.Left,
            NoWrap = true,
            Padding = new Padding(0, 0, 1, 0)
        };

        var detailsColumn = new TableColumn(string.Empty)
        {
            Alignment = Justify.Left
        };

        var table = new Table
        {
            Expand = true,
            Border = TableBorder.Simple
        };

        table.ShowHeaders = false;
        table.AddColumn(timestampColumn);
        table.AddColumn(detailsColumn);

        return table;
    }

    private static string BuildTimestampMarkup(JObject entry)
    {
        var rawTimestamp = entry["Timestamp"]?.ToString() ?? string.Empty;
        var parsedTimestamp = ParseTimestamp(rawTimestamp);
        var timestampDisplay = parsedTimestamp.HasValue
            ? FormatTimestamp(parsedTimestamp.Value)
            : rawTimestamp;

        if (string.IsNullOrWhiteSpace(timestampDisplay))
        {
            timestampDisplay = "—";
        }

        return BoldValue(timestampDisplay);
    }

    private static string BuildDetailsMarkup(JObject entry)
    {
        var segments = BuildSegments(entry)
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToList();

        if (segments.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(SegmentSeparator, segments);
    }

    private static IEnumerable<string> BuildSegments(JObject entry)
    {
        var segments = new List<string>();

        var type = entry["Type"]?.ToString();
        var category = entry["Category"]?.ToString();

        AddIfNotNull(segments, BuildLabeledValue("Type", type));
        AddIfNotNull(segments, BuildLabeledValue("Category", category));

        if (IsSleepSession(entry))
        {
            segments.AddRange(BuildSleepSegments(entry));
            return segments;
        }

        if (string.Equals(type, "Measurement", StringComparison.OrdinalIgnoreCase))
        {
            segments.AddRange(BuildMeasurementSegments(entry));
        }
        else if (string.Equals(type, "Consumption", StringComparison.OrdinalIgnoreCase))
        {
            segments.AddRange(BuildConsumptionSegments(entry));
        }
        else if (string.Equals(type, "Note", StringComparison.OrdinalIgnoreCase))
        {
            segments.AddRange(BuildNoteSegments(entry));
        }
        else
        {
            segments.AddRange(BuildGenericSegments(entry));
        }

        return segments;
    }

    private static bool IsSleepSession(JObject entry)
    {
        var category = entry["Category"]?.ToString();
        return string.Equals(category, "Sleep", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> BuildSleepSegments(JObject entry)
    {
        var segments = new List<string>();

        var durationSegment = BuildSleepDurationSegment(entry);
        if (durationSegment is not null)
        {
            segments.Add(durationSegment);
        }


        AddIfNotNull(segments, BuildLabeledNumber("Score", ExtractDouble(entry["Score"])));
        AddIfNotNull(segments, BuildLabeledNumber("Efficiency", ExtractDouble(entry["Efficiency"]), "%"));
        AddIfNotNull(segments, BuildLabeledNumber("Wake Score", ExtractDouble(entry["WakeScore"])));

        AddIfNotNull(segments, BuildLabeledNumber("Mental Recovery", ExtractDouble(entry["MentalRecovery"])));
        AddIfNotNull(segments, BuildLabeledNumber("Physical Recovery", ExtractDouble(entry["PhysicalRecovery"])));

        AddIfNotNull(segments, BuildStageSegment("Awake", ExtractDouble(entry["AwakeDurationMinutes"])));
        AddIfNotNull(segments, BuildStageSegment("REM", ExtractDouble(entry["REMDurationMinutes"])));
        AddIfNotNull(segments, BuildStageSegment("Light", ExtractDouble(entry["LightDurationMinutes"])));

        // Always show Deep duration, even if it's 0
        var deepDuration = ExtractDouble(entry["DeepDurationMinutes"]);
        if (deepDuration.HasValue)
        {
            var deepText = FormatDurationMinutes(deepDuration);
            AddIfNotNull(segments, BuildLabeledValue("Deep", deepText));
        }

        // Handle legacy sleep measurements that don't have consolidated sleep session fields
        if (segments.Count == 0)
        {
            var name = entry["Name"]?.ToString();
            var valueToken = entry["Value"];
            var numericValue = ExtractDouble(valueToken);

            if (!string.IsNullOrWhiteSpace(name))
            {
                string? displayValue = null;

                if (numericValue.HasValue)
                {
                    displayValue = FormatNumber(numericValue.Value);
                }
                else if (valueToken is not null && valueToken.Type != JTokenType.Null)
                {
                    var textValue = valueToken.ToString();
                    if (!string.IsNullOrWhiteSpace(textValue))
                    {
                        displayValue = textValue;
                    }
                }

                var unit = entry["Unit"]?.ToString();
                if (!string.IsNullOrWhiteSpace(unit))
                {
                    displayValue = displayValue is not null
                        ? $"{displayValue} {unit}"
                        : unit;
                }

                var segment = displayValue is not null
                    ? BuildLabeledValue(name, displayValue)
                    : Markup.Escape(name);

                if (!string.IsNullOrWhiteSpace(segment))
                {
                    segments.Add(segment);
                }
            }
        }

        return segments;
    }

    private static IEnumerable<string> BuildMeasurementSegments(JObject entry)
    {
        var segments = new List<string>();

        var name = entry["Name"]?.ToString();
        var valueToken = entry["Value"];
        var numericValue = ExtractDouble(valueToken);

        if (!string.IsNullOrWhiteSpace(name))
        {
            string? displayValue = null;

            if (numericValue.HasValue)
            {
                displayValue = FormatNumber(numericValue.Value);
            }
            else if (valueToken is not null && valueToken.Type != JTokenType.Null)
            {
                var textValue = valueToken.ToString();
                if (!string.IsNullOrWhiteSpace(textValue))
                {
                    displayValue = textValue;
                }
            }

            var unit = entry["Unit"]?.ToString();
            if (!string.IsNullOrWhiteSpace(unit))
            {
                displayValue = displayValue is not null
                    ? $"{displayValue} {unit}"
                    : unit;
            }

            var segment = displayValue is not null
                ? BuildLabeledValue(name!, displayValue)
                : Markup.Escape(name!);

            if (!string.IsNullOrWhiteSpace(segment))
            {
                segments.Add(segment!);
            }
        }

        return segments;
    }

    private static IEnumerable<string> BuildConsumptionSegments(JObject entry)
    {
        var segments = new List<string>();

        AddIfNotNull(segments, BuildLabeledValue("Name", entry["Name"]?.ToString()));

        var amount = ExtractDouble(entry["Amount"]);
        var unit = entry["Unit"]?.ToString();
        if (amount.HasValue || !string.IsNullOrWhiteSpace(unit))
        {
            var amountText = amount.HasValue ? FormatNumber(amount.Value) : null;
            if (!string.IsNullOrWhiteSpace(unit))
            {
                amountText = amountText is not null
                    ? $"{amountText} {unit}"
                    : unit;
            }

            var amountSegment = BuildLabeledValue("Amount", amountText);
            AddIfNotNull(segments, amountSegment);
        }

        return segments;
    }

    private static IEnumerable<string> BuildNoteSegments(JObject entry)
    {
        var segments = new List<string>();
        var contentSegment = BuildLabeledValue("Content", entry["Content"]?.ToString());
        AddIfNotNull(segments, contentSegment);
        return segments;
    }

    private static IEnumerable<string> BuildGenericSegments(JObject entry)
    {
        var segments = new List<string>();

        foreach (var property in entry.Properties())
        {
            if (string.Equals(property.Name, "Timestamp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(property.Name, "Type", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(property.Name, "Category", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value is JObject or JArray)
            {
                continue;
            }

            var value = property.Value?.ToString();
            var segment = BuildLabeledValue(property.Name, value);
            AddIfNotNull(segments, segment);
        }

        return segments;
    }

    private static string? BuildSleepDurationSegment(JObject entry)
    {
        var durationMinutes = ExtractDouble(entry["DurationMinutes"]);

        if (!durationMinutes.HasValue)
        {
            return null;
        }

        var durationText = FormatDurationMinutes(durationMinutes);
        if (string.IsNullOrWhiteSpace(durationText))
        {
            return null;
        }

        return BuildLabeledValue("Duration", durationText);
    }

    private static string? BuildStageSegment(string label, double? minutes)
    {
        if (!minutes.HasValue || minutes.Value <= 0.01)
        {
            return null;
        }

        var durationText = FormatDurationMinutes(minutes);
        return BuildLabeledValue(label, durationText);
    }

    private static string? FormatDurationMinutes(double? minutes)
    {
        if (!minutes.HasValue)
        {
            return null;
        }

        var totalMinutes = Math.Round(minutes.Value, 2, MidpointRounding.AwayFromZero);
        if (totalMinutes <= 0)
        {
            return "0m";
        }

        var span = TimeSpan.FromMinutes(totalMinutes);
        var hours = (int)Math.Floor(span.TotalHours);
        var mins = span.Minutes;
        var seconds = span.Seconds;

        var components = new List<string>();
        if (hours > 0)
        {
            components.Add($"{hours}h");
        }

        if (mins > 0 || components.Count == 0)
        {
            components.Add($"{mins}m");
        }

        if (components.Count == 0 && seconds > 0)
        {
            components.Add($"{seconds}s");
        }

        return string.Join(" ", components);
    }

    private static string? BuildTimeRangeText(DateTimeOffset? start, DateTimeOffset? end)
    {
        if (!start.HasValue && !end.HasValue)
        {
            return null;
        }

        var culture = CultureInfo.CurrentCulture;

        string FormatTime(DateTimeOffset? value) =>
            value.HasValue ? value.Value.ToLocalTime().ToString("HH:mm", culture) : "??";

        return $"({FormatTime(start)} → {FormatTime(end)})";
    }

    private static double? ExtractDouble(JObject source, string propertyName)
    {
        return source.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out var token)
            ? ExtractDouble(token)
            : null;
    }

    private static double? ExtractDouble(JToken? token)
    {
        if (token is null || token.Type == JTokenType.Null)
        {
            return null;
        }

        if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
        {
            return token.Value<double>();
        }

        var raw = token.ToString();
        if (double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var invariant))
        {
            return invariant;
        }

        if (double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var cultureValue))
        {
            return cultureValue;
        }

        return null;
    }

    private static string FormatNumber(double value)
    {
        var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        if (Math.Abs(rounded - Math.Round(rounded)) < 0.01)
        {
            return Math.Round(rounded).ToString(CultureInfo.CurrentCulture);
        }

        return rounded.ToString("0.##", CultureInfo.CurrentCulture);
    }

    private static string BoldValue(string text)
    {
        return $"[bold]{Markup.Escape(text)}[/]";
    }

    private static string? BuildLabeledValue(string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return $"{Markup.Escape(label)}: {BoldValue(value)}";
    }

    private static string? BuildLabeledNumber(string label, double? value, string? unit = null)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var formatted = FormatNumber(value.Value);
        if (!string.IsNullOrWhiteSpace(unit))
        {
            formatted = $"{formatted} {unit}";
        }

        return BuildLabeledValue(label, formatted);
    }

    private static void AddIfNotNull(List<string> segments, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            segments.Add(value);
        }
    }

    private static List<JObject> SortEntriesByTimestamp(IEnumerable<JObject> entries)
    {
        return entries
            .OrderBy(BuildSortKey)
            .ToList();
    }

    private static (int Priority, DateTimeOffset Timestamp, string RawTimestamp, string Type, string Category, string Name) BuildSortKey(JObject entry)
    {
        var raw = entry["Timestamp"]?.ToString() ?? string.Empty;
        var parsed = ParseTimestamp(raw);
        var type = entry["Type"]?.ToString() ?? string.Empty;
        var category = entry["Category"]?.ToString() ?? string.Empty;
        var name = entry["Name"]?.ToString() ?? string.Empty;

        return (parsed.HasValue ? 0 : 1, parsed ?? DateTimeOffset.MinValue, raw, type, category, name);
    }

    private static DateTimeOffset? ParseTimestamp(string? timestamp)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
        {
            return null;
        }

        if (DateTimeOffset.TryParseExact(timestamp, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
        {
            return dto;
        }

        var dateTimeFormats = new[]
        {
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy",
            "d/M/yyyy HH:mm:ss",
            "d/M/yyyy HH:mm",
            "d/M/yyyy",
            "MM/dd/yyyy HH:mm:ss",
            "MM/dd/yyyy HH:mm",
            "MM/dd/yyyy",
            "M/d/yyyy HH:mm:ss",
            "M/d/yyyy HH:mm",
            "M/d/yyyy"
        };

        if (DateTimeOffset.TryParseExact(timestamp, dateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dto) ||
            DateTimeOffset.TryParseExact(timestamp, dateTimeFormats, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dto))
        {
            return dto;
        }

        if (DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dto) ||
            DateTimeOffset.TryParse(timestamp, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dto))
        {
            return dto;
        }

        if (DateTime.TryParseExact(timestamp, dateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDateTime) ||
            DateTime.TryParseExact(timestamp, dateTimeFormats, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out parsedDateTime) ||
            DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsedDateTime) ||
            DateTime.TryParse(timestamp, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out parsedDateTime))
        {
            if (parsedDateTime.Kind == DateTimeKind.Unspecified)
            {
                parsedDateTime = DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Local);
            }

            return new DateTimeOffset(parsedDateTime);
        }

        return null;
    }

    private static string FormatTimestamp(DateTimeOffset timestamp)
    {
        var culture = CultureInfo.CurrentCulture;
        var localTimestamp = timestamp.ToLocalTime();
        var datePart = localTimestamp.ToString(culture.DateTimeFormat.ShortDatePattern, culture);
        var timePart = localTimestamp.ToString(culture.DateTimeFormat.LongTimePattern, culture);
        return $"{datePart} {timePart}";
    }
}