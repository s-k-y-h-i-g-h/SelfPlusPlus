using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using SelfPlusPlusCLI.Common;
using Spectre.Console.Json;

namespace SelfPlusPlusCLI.Show;

public class ShowCommand : Command<ShowSettings>
{
    private readonly IConfiguration _configuration;
    private readonly LogDataService _logDataService;

    public ShowCommand(IConfiguration configuration, LogDataService logDataService)
    {
        _configuration = configuration;
        _logDataService = logDataService;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] ShowSettings settings)
    {
        if(settings.Format == Format.JSON)
        {
            var jsonText = new JsonText(_logDataService.ToJsonString());
            AnsiConsole.Write(jsonText);            
        }
        
        if(settings.ShowPath)
        {
            var path = _logDataService.GetLogDataFilePath();
            AnsiConsole.Write(path);
        }

        return 0;
    }
}