using System.ComponentModel;
using Spectre.Console.Cli;

namespace SelfPlusPlusCLI.Show;

public class ShowSettings : CommandSettings
{    
    [Description(".")]
    [CommandOption("--format")]
    public Format? Format { get; set;  }

    [Description(".")]
    [CommandOption("--show-path")]
    [DefaultValue(false)]
    public bool ShowPath { get; set;  }
}
