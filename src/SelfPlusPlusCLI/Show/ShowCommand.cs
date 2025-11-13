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
            
            // Filter by start-date and start-time if provided
            if (!string.IsNullOrWhiteSpace(settings.StartDate) || !string.IsNullOrWhiteSpace(settings.StartTime))
            {
                DateTime localDate;
                
                if (!string.IsNullOrWhiteSpace(settings.StartDate))
                {
                    if (DateTime.TryParseExact(settings.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var isoDate))
                    {
                        localDate = isoDate;
                    }
                    else if (DateTime.TryParseExact(settings.StartDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dmy))
                    {
                        localDate = dmy;
                    }
                    else if (DateTime.TryParse(settings.StartDate, CultureInfo.CurrentCulture, DateTimeStyles.None, out var cultureDate))
                    {
                        localDate = cultureDate.Date;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]Error: Unable to parse --start-date. Use format yyyy-MM-dd or dd/MM/yyyy.[/]");
                        return 1;
                    }
                }
                else
                {
                    // If only start-time is provided, use today's date
                    localDate = DateTime.Now.Date;
                }
                
                TimeSpan timeOfDay = TimeSpan.Zero;
                if (!string.IsNullOrWhiteSpace(settings.StartTime))
                {
                    if (TimeSpan.TryParse(settings.StartTime, out var parsedTime))
                    {
                        timeOfDay = parsedTime;
                    }
                    else if (DateTime.TryParseExact(settings.StartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeOnly))
                    {
                        timeOfDay = timeOnly.TimeOfDay;
                    }
                    else if (DateTime.TryParseExact(settings.StartTime, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeWithSeconds))
                    {
                        timeOfDay = timeWithSeconds.TimeOfDay;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]Error: Unable to parse --start-time. Use format HH:mm or HH:mm:ss.[/]");
                        return 1;
                    }
                }
                
                var localDateTime = localDate.Add(timeOfDay);
                var timeZone = TimeZoneInfo.Local;
                var startFilter = new DateTimeOffset(localDateTime, timeZone.GetUtcOffset(localDateTime));
                var startUtc = startFilter.ToUniversalTime();
                
                var filteredEntries = new List<JObject>();
                
                foreach (var entry in entries)
                {
                    var timestampStr = entry["Timestamp"]?.ToString();
                    if (string.IsNullOrWhiteSpace(timestampStr)) continue;
                    
                    if (DateTimeOffset.TryParseExact(timestampStr, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var entryDto))
                    {
                        var entryUtc = entryDto.ToUniversalTime();
                        if (entryUtc >= startUtc)
                        {
                            filteredEntries.Add(entry);
                        }
                    }
                    else if (DateTimeOffset.TryParse(timestampStr, out var parsedDto))
                    {
                        var entryUtc = parsedDto.ToUniversalTime();
                        if (entryUtc >= startUtc)
                        {
                            filteredEntries.Add(entry);
                        }
                    }
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