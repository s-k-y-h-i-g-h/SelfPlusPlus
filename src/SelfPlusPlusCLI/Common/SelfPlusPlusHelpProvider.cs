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
        yield return new Markup(":chart_increasing:[bold]SelfPlusPlusCLI[/] [italic]v0.1[/]\n\n");

        yield return new Markup("ABOUT:\n");
        yield return new Markup("    Known as [bold]SelfPlusPlus[/], [bold]Self++[/] and [bold]spp[/]\n\n");
        yield return new Markup("    [dim]Released under the GNU GPL v3.0 License[/]\n");
        yield return new Markup("    [dim]See LICENSE file for full license text[/]\n");
        yield return new Markup("    [dim]:copyright: George Zankevich 2025[/]\n\n");
    }

}