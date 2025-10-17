using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Add;

public class AddMeasurementCommand : Command<AddMeasurementSettings>
{
    private readonly IConfiguration _configuration;

    public AddMeasurementCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] AddMeasurementSettings settings)
    {
        AnsiConsole.WriteLine($"Adding measurement: {settings.Name} {settings.Value} {settings.Unit}");
        return 0;
    }
}