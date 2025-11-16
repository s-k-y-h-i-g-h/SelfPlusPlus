using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SelfPlusPlusCLI;
using SelfPlusPlusCLI.Common;
using SelfPlusPlusCLI.Add;
using SelfPlusPlusCLI.Show;
using SelfPlusPlusCLI.Import;
using Spectre.Console;
using Spectre.Console.Cli;

var builder = Host.CreateDefaultBuilder(args);

// Add services to the container
builder.ConfigureServices(services =>
{
    services.AddSingleton<IAnsiConsole>(_ => AnsiConsole.Console);
    services.AddSingleton<ILogDataPathProvider, DefaultLogDataPathProvider>();
    services.AddSingleton<LogDataService>();
});

var registrar = new TypeRegistrar(builder);

var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.AddBranch<AddSettings>("add", add =>
    {
        add.AddCommand<ConsumptionCommand>("consumption")
            .WithDescription("    Add a :mouth:consumption log entry. [bold]<CATEGORY>[/] can be :pill:Substance, :red_apple:Food, or :package:Stack (case insensitive).")
            .WithExample(new[] { "add", "consumption", "substance", "Coffee", "1", "cup" });

        add.AddCommand<MeasurementCommand>("measurement")
            .WithDescription("    Add a :triangular_ruler: measurement log entry.")
            .WithExample(new[] { "add", "measurement", "\"Heart Rate\"", "70", "BPM" });

        add.AddCommand<NoteCommand>("note")
            .WithDescription("    Add a :memo: note log entry.")
            .WithExample(new[] { "add", "note", "\"Felt great after workout\"" });
    });

    config.AddCommand<ShowCommand>("show")
        .WithDescription("Show log entries.");

    config.AddCommand<ImportCommand>("import")
        .WithDescription("Import log entries from a data source.");

    config.SetHelpProvider(new SelfPlusPlusHelpProvider(config.Settings));
});

return app.Run(args);