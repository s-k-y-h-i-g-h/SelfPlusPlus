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

    [Description("Filter entries from this date (format: yyyy-MM-dd or dd/MM/yyyy).")]
    [CommandOption("--start-date")]
    public string? StartDate { get; set;  }

    [Description("Filter entries from this time (format: HH:mm or HH:mm:ss). Used with --start-date.")]
    [CommandOption("--start-time")]
    public string? StartTime { get; set;  }

    [CommandOption("--end-date")]
    public string? EndDate { get; set;  }
}
