using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
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
        AnsiConsole.WriteLine($"Adding consumption: {settings.Category} {settings.Name} {settings.Amount} {settings.Unit}");
        return 0;
    }
}