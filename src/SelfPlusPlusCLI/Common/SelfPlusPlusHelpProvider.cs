using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using Spectre.Console.Cli.Help;

namespace SelfPlusPlusCLI.Common;

class SelfPlusPlusHelpProvider : HelpProvider
{
    public SelfPlusPlusHelpProvider(ICommandAppSettings settings) : base(settings)
    {

    }

    // App-level help header (shown for `--help` without a command)
    public override IEnumerable<IRenderable> GetHeader(ICommandModel model, ICommandInfo? command)
    {
        yield return new Markup(":chart_increasing:[bold]SelfPlusPlusCLI[/] [italic]v0.1[/]\n");
        yield return new Align(
            new Markup("[italic]You plus a bit more![/] [bold][/]\n"), 
            HorizontalAlignment.Left
        );
        yield return new Markup("ABOUT:\n");
        yield return new Markup("    Known as [bold]SelfPlusPlus[/], [bold]Self++[/] and [bold]spp[/]\n\n");
        yield return new Markup("    :copyright: George Zankevich 2025\n\n");
    }

}