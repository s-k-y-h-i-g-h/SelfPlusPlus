using System.Diagnostics.CodeAnalysis;
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
            
            var table = new Table();
            table.AddColumn("Timestamp");
            table.AddColumn("Type");
            table.AddColumn("Category");
            table.AddColumn(new TableColumn("Name").Centered());
            table.AddColumn(new TableColumn("Amount").Centered());
            table.AddColumn(new TableColumn("Value").Centered());
            table.AddColumn(new TableColumn("Unit").Centered());
            
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