using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using SelfPlusPlusCLI.Common;
using Spectre.Console.Json;
using Newtonsoft.Json.Linq;

namespace SelfPlusPlusCLI.Show;

public class ShowCommand : Command<ShowSettings>
{
    private readonly IConfiguration _configuration;
    private readonly LogDataService _logDataService;

    public ShowCommand(IConfiguration configuration, LogDataService logDataService)
    {
        _configuration = configuration;
        _logDataService = logDataService;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] ShowSettings settings)
    {
        if(settings.Format == Format.JSON)
        {
            var jsonText = new JsonText(_logDataService.ToJsonString());
            AnsiConsole.Write(jsonText);            
        } 
        else if(settings.ShowPath)
        {
            var path = _logDataService.GetLogDataFilePath();
            AnsiConsole.Write(path);
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
                AnsiConsole.MarkupLine(startError!);
                return 1;
            }

            if (!TryCreateBoundary(settings.EndDate, settings.EndTime, true, "--end-date", "--end-time", out var endBoundary, out var endError))
            {
                AnsiConsole.MarkupLine(endError!);
                return 1;
            }

            var startUtc = startBoundary?.ToUniversalTime();
            var endUtc = endBoundary?.ToUniversalTime();

            if (startUtc.HasValue && endUtc.HasValue && startUtc > endUtc)
            {
                AnsiConsole.MarkupLine("[red]Error: --start-date/time must be earlier than --end-date/time.[/]");
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

                    DateTimeOffset entryUtc;

                    if (DateTimeOffset.TryParseExact(timestampStr, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var entryDto))
                    {
                        entryUtc = entryDto.ToUniversalTime();
                    }
                    else if (DateTimeOffset.TryParse(timestampStr, out var parsedDto))
                    {
                        entryUtc = parsedDto.ToUniversalTime();
                    }
                    else
                    {
                        continue;
                    }

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

            var table = new Table();
            table.AddColumn("Timestamp");
            table.AddColumn("Type");
            table.AddColumn("Category");
            table.AddColumn(new TableColumn("Name"));
            table.AddColumn(new TableColumn("Amount"));
            table.AddColumn(new TableColumn("Value"));
            table.AddColumn(new TableColumn("Unit"));
            
            foreach(var entry in entries)
            {
                var timestamp = entry["Timestamp"]?.ToString() ?? string.Empty;
                var type = entry["Type"]?.ToString() ?? string.Empty;
                var category = entry["Category"]?.ToString() ?? string.Empty;
                var name = entry["Name"]?.ToString() ?? string.Empty;
                var amount = entry["Amount"]?.ToString() ?? string.Empty;
                var value = entry["Value"]?.ToString() ?? string.Empty;
                var unit = entry["Unit"]?.ToString() ?? string.Empty;
                
                table.AddRow(timestamp, type, category, name, amount, value, unit);
            }
            
            AnsiConsole.Write(table);
        }

        return 0;
    }
}