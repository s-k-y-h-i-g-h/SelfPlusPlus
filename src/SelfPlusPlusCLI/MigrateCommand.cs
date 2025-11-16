using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Cli;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI;

public sealed class MigrateCommand : Command<MigrateSettings>
{
    private readonly LogDataService _logDataService;
    private readonly IAnsiConsole _console;

    public MigrateCommand(LogDataService logDataService, IAnsiConsole console)
    {
        _logDataService = logDataService;
        _console = console;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] MigrateSettings settings)
    {
        try
        {
            var entries = _logDataService.ReadLogEntries();
            var migratedCount = 0;

            foreach (var entry in entries)
            {
                var type = entry["Type"]?.ToString();
                var category = entry["Category"]?.ToString();

                if (string.Equals(type, "Measurement", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(category, "Sleep", StringComparison.OrdinalIgnoreCase))
                {
                    // Migrate StageMinutes to flattened fields
                    if (entry["StageMinutes"] is JObject stageMinutes)
                    {
                        entry["AwakeDuration"] = stageMinutes["Awake"];
                        entry["RemDuration"] = stageMinutes["Rem"];
                        entry["LightDuration"] = stageMinutes["Light"];
                        entry["DeepDuration"] = stageMinutes["Deep"];
                        entry["UnmappedDuration"] = stageMinutes["Unmapped"];

                        // Remove the old nested object
                        entry.Remove("StageMinutes");
                        migratedCount++;
                    }

                    // Migrate RecoveryScores to flattened fields
                    if (entry["RecoveryScores"] is JObject recoveryScores)
                    {
                        entry["MentalRecovery"] = recoveryScores["Mental"];
                        entry["PhysicalRecovery"] = recoveryScores["Physical"];

                        // Remove the old nested object
                        entry.Remove("RecoveryScores");
                        migratedCount++;
                    }
                }
            }

            if (migratedCount > 0)
            {
                _logDataService.WriteLogEntries(entries);
                _console.MarkupLine($"[green]Migrated {migratedCount} sleep log entries to flattened structure.[/]");
            }
            else
            {
                _console.MarkupLine("[yellow]No sleep entries found that needed migration.[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]Failed to migrate log entries:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}

public class MigrateSettings : CommandSettings
{
}
