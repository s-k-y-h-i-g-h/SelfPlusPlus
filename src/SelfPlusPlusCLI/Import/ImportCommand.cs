using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Cli;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Import;

public class ImportCommand : Command<ImportSettings>
{
    private readonly IConfiguration _configuration;
    private readonly LogDataService _logDataService;
    private readonly IAnsiConsole _console;

    public ImportCommand(IConfiguration configuration, LogDataService logDataService, IAnsiConsole console)
    {
        _configuration = configuration;
        _logDataService = logDataService;
        _console = console;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] ImportSettings settings)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.SamsungHealthDirectory))
            {
                _console.MarkupLine("[red]Error:[/] Samsung Health directory was not provided. Use --samsung-health-directory to specify the path.");
                return 1;
            }

            var importer = new SamsungHealthImporter();
            var result = importer.Import(settings.SamsungHealthDirectory, _logDataService);

            if (result.MeasurementsAdded == 0)
            {
                _console.MarkupLine("[yellow]No new Samsung Health sleep measurements were imported.[/]");
            }
            else
            {
                _console.MarkupLine($"[green]Imported {result.MeasurementsAdded} measurement entries across {result.SessionsProcessed} sleep sessions.[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]Failed to import Samsung Health data:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}