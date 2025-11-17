using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Cli;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Add;

public class MeasurementCommand : Command<MeasurementSettings>
{
    private readonly IConfiguration _configuration;
    private readonly LogDataService _logDataService;
    private readonly IAnsiConsole _console;

    public MeasurementCommand(IConfiguration configuration, LogDataService logDataService, IAnsiConsole console)
    {
        _configuration = configuration;
        _logDataService = logDataService;
        _console = console;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] MeasurementSettings settings)
    {
        try
        {
            var unit = settings.Unit ?? string.Empty;
            var logEntry = new MeasurementLogEntry
            {
                Category = settings.Category.ToString(),
                Name = settings.Name,
                Value = settings.Value,
                Unit = unit
            };

            var entryObject = JObject.FromObject(logEntry);
            entryObject["$type"] = "MeasurementLogEntry";
            _logDataService.AddLogEntry(entryObject);

            var unitDisplay = string.IsNullOrWhiteSpace(unit) ? string.Empty : $" {unit}";
            _console.MarkupLine($"[green]Added measurement entry:[/] {settings.Category} {settings.Name} {settings.Value}{unitDisplay}");
            return 0;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]Failed to add measurement entry:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}