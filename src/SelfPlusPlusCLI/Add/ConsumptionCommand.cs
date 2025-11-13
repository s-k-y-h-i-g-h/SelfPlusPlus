using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Cli;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Add;

public class ConsumptionCommand : Command<ConsumptionSettings>
{
    private readonly IConfiguration _configuration;
    private readonly LogDataService _logDataService;
    private readonly IAnsiConsole _console;

    public ConsumptionCommand(IConfiguration configuration, LogDataService logDataService, IAnsiConsole console)
    {
        _configuration = configuration;
        _logDataService = logDataService;
        _console = console;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] ConsumptionSettings settings)
    {
        try
        {
            var logEntry = new ConsumptionLogEntry
            {
                Category = settings.Category.ToString(),
                Name = settings.Name,
                Amount = settings.Amount,
                Unit = settings.Unit
            };

            var entryObject = JObject.FromObject(logEntry);
            _logDataService.AddLogEntry(entryObject);

            var amountFragment = settings.Amount.HasValue
                ? $" {settings.Amount.Value.ToString(CultureInfo.InvariantCulture)} {(settings.Unit ?? string.Empty)}".TrimEnd()
                : string.Empty;

            _console.MarkupLine($"[green]Added consumption entry:[/] {settings.Category} {settings.Name}{amountFragment}");
            return 0;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]Failed to add consumption entry:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}