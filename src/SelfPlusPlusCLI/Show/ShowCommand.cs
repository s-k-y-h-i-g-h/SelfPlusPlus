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
                if (string.IsNullOrWhiteSpace(timestamp))
                {
                    return null;
                }

                if (DateTimeOffset.TryParseExact(timestamp, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var entryDto))
                {
                    return entryDto;
                }

                if (DateTimeOffset.TryParse(timestamp, out var parsedDto))
                {
                    return parsedDto;
                }

                return null;
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

            if (settings.Format == Format.JSON)
            {
                var jsonArray = new JArray(entries);
                var jsonText = new JsonText(jsonArray.ToString(Newtonsoft.Json.Formatting.Indented));
                _console.Write(jsonText);
                return 0;
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

            var table = new Table();
            table.AddColumn("Timestamp");
            table.AddColumn("Type");
            table.AddColumn("Category");
            table.AddColumn(new TableColumn("Name"));
            table.AddColumn(new TableColumn("Amount"));
            table.AddColumn(new TableColumn("Value"));
            table.AddColumn(new TableColumn("Unit"));
            table.AddColumn(new TableColumn("Content"));
            
            foreach(var entry in entries)
            {
                var timestamp = entry["Timestamp"]?.ToString() ?? string.Empty;
                var type = entry["Type"]?.ToString() ?? string.Empty;
                var category = entry["Category"]?.ToString() ?? string.Empty;
                var name = entry["Name"]?.ToString() ?? string.Empty;
                var amount = entry["Amount"]?.ToString() ?? string.Empty;
                var value = entry["Value"]?.ToString() ?? string.Empty;
                var unit = entry["Unit"]?.ToString() ?? string.Empty;
                var content = entry["Content"]?.ToString() ?? string.Empty;
                
                table.AddRow(
                    Markup.Escape(timestamp),
                    Markup.Escape(type),
                    Markup.Escape(category),
                    Markup.Escape(name),
                    Markup.Escape(amount),
                    Markup.Escape(value),
                    Markup.Escape(unit),
                    Markup.Escape(content));
            }
            
            _console.Write(table);
        }

        return 0;
    }
}