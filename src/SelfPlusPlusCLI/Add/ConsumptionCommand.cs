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

    public ConsumptionCommand(IConfiguration configuration, LogDataService logDataService)
    {
        _configuration = configuration;
        _logDataService = logDataService;
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

            AnsiConsole.MarkupLine($"[green]Added consumption entry:[/] {settings.Category} {settings.Name}{amountFragment}");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to add consumption entry:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}