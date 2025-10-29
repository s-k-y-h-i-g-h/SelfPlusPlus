using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Add;

public class MeasurementCommand : Command<MeasurementSettings>
{
    private readonly IConfiguration _configuration;

    public MeasurementCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] MeasurementSettings settings)
    {
        AnsiConsole.WriteLine($"Adding measurement: {settings.Name} {settings.Value} {settings.Unit}");
        return 0;
    }
}