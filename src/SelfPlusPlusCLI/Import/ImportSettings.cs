using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SelfPlusPlusCLI.Import;

public class ImportSettings : CommandSettings
{
    [Description("Path to the Samsung Health export directory.")]
    [CommandOption("--samsung-health-directory")]
    public string? SamsungHealthDirectory { get; set; }
}
