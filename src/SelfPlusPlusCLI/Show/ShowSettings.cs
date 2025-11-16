using System.ComponentModel;
using Spectre.Console.Cli;

namespace SelfPlusPlusCLI.Show;

public class ShowSettings : CommandSettings
{    
    [Description("Format to display the log data in (values: JSON).")]
    [CommandOption("--format")]
    public Format? Format { get; set;  }

    [Description("Show the path of the log data file.")]
    [CommandOption("--show-path")]
    [DefaultValue(false)]
    public bool ShowPath { get; set;  }

    [Description("Filter entries from this date (format: yyyy-MM-dd or dd/MM/yyyy).")]
    [CommandOption("--start-date")]
    public string? StartDate { get; set;  }

    [Description("Filter entries from this time (format: HH:mm or HH:mm:ss). Used with --start-date.")]
    [CommandOption("--start-time")]
    public string? StartTime { get; set;  }

    [Description("Filter entries until this time (format: HH:mm or HH:mm:ss). Used with --end-date.")]
    [CommandOption("--end-time")]
    public string? EndTime { get; set;  }

    [Description("Filter entries until this date (format: yyyy-MM-dd or dd/MM/yyyy).")]
    [CommandOption("--end-date")]
    public string? EndDate { get; set;  }
    
    [Description("Show total of consumption entries per day.")]
    [CommandOption("--total")]
    [DefaultValue(false)]
    public bool Total { get; set;  }

    [Description("Filter entries by category (case-insensitive).")]
    [CommandOption("--category")]
    public string? Category { get; set; }

    [Description("Filter entries by type (case-insensitive).")]
    [CommandOption("--type")]
    public string? EntryType { get; set; }
}
