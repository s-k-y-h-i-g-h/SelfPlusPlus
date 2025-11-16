using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Cli;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Add;

public sealed class NoteCommand : Command<NoteSettings>
{
    private readonly IConfiguration _configuration;
    private readonly LogDataService _logDataService;
    private readonly IAnsiConsole _console;

    public NoteCommand(IConfiguration configuration, LogDataService logDataService, IAnsiConsole console)
    {
        _configuration = configuration;
        _logDataService = logDataService;
        _console = console;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] NoteSettings settings)
    {
        try
        {
            var category = (settings.Category ?? string.Empty).Trim();
            var content = (settings.Content ?? string.Empty).Trim();

            var logEntry = new NoteLogEntry
            {
                Category = category,
                Content = content
            };

            var entryObject = JObject.FromObject(logEntry);
            _logDataService.AddLogEntry(entryObject);

            _console.MarkupLine($"[green]Added note entry:[/] {Markup.Escape(category)} - {Markup.Escape(content)}");
            return 0;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]Failed to add note entry:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}


