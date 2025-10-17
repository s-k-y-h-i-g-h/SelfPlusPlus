using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Add;

public class AddConsumptionCommand : Command<AddConsumptionSettings>
{
    private readonly IConfiguration _configuration;

    public AddConsumptionCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] AddConsumptionSettings settings)
    {
        AnsiConsole.WriteLine($"Adding consumption: {settings.Category} {settings.Name} {settings.Amount} {settings.Unit}");
        return 0;
    }
}