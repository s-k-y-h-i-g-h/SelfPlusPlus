using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Add;

public class AddCommand : Command<AddSettings>
{
    private readonly IConfiguration _configuration;

    public AddCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] AddSettings settings)
    {
        return 0;
    }
}