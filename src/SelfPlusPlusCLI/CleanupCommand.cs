using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Cli;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI;

public class CleanupCommand : Command<CleanupSettings>
{
    private readonly IConfiguration _configuration;
    private readonly LogDataService _logDataService;
    private readonly IAnsiConsole _console;

    public CleanupCommand(IConfiguration configuration, LogDataService logDataService, IAnsiConsole console)
    {
        _configuration = configuration;
        _logDataService = logDataService;
        _console = console;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] CleanupSettings settings)
    {
        try
        {
            var entries = _logDataService.ReadLogEntries();
            var cleanedCount = 0;

            foreach (var entry in entries)
            {
                var entryChanged = false;

                // Remove deprecated fields from SleepLogEntry
                if (entry.ContainsKey("Source"))
                {
                    entry.Remove("Source");
                    entryChanged = true;
                }

                if (entry.ContainsKey("SourceId"))
                {
                    entry.Remove("SourceId");
                    entryChanged = true;
                }

                if (entry.ContainsKey("Notes"))
                {
                    entry.Remove("Notes");
                    entryChanged = true;
                }

                // Remove Quality field from sleep entries
                if (entry["Type"]?.ToString() == "Measurement" &&
                    entry["Category"]?.ToString() == "Sleep" &&
                    entry.ContainsKey("Quality"))
                {
                    entry.Remove("Quality");
                    entryChanged = true;
                }

                // Remove Start and End fields from sleep entries
                if (entry["Type"]?.ToString() == "Measurement" &&
                    entry["Category"]?.ToString() == "Sleep")
                {
                    if (entry.ContainsKey("Start"))
                    {
                        entry.Remove("Start");
                        entryChanged = true;
                    }
                    if (entry.ContainsKey("End"))
                    {
                        entry.Remove("End");
                        entryChanged = true;
                    }
                }

                // Remove Name field from sleep entries (but keep Name for other measurement types)
                if (entry["Type"]?.ToString() == "Measurement" &&
                    entry["Category"]?.ToString() == "Sleep" &&
                    entry.ContainsKey("Name"))
                {
                    entry.Remove("Name");
                    entryChanged = true;
                }

                if (entryChanged)
                {
                    cleanedCount++;
                }
            }

            if (cleanedCount > 0)
            {
                // Re-save the cleaned entries
                _logDataService.WriteLogEntries(entries);
                _console.MarkupLine($"[green]Cleaned {cleanedCount} log entries by removing deprecated fields.[/]");
            }
            else
            {
                _console.MarkupLine("[yellow]No deprecated fields found to clean up.[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]Failed to clean up log entries:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}

public class CleanupSettings : CommandSettings
{
}
