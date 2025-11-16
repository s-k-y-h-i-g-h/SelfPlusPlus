using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using SelfPlusPlusCLI.Add;
using SelfPlusPlusCLI.Common;
using SelfPlusPlusCLI.Show;
using SelfPlusPlusCLI.Tests.Support;
using Spectre.Console;
using Spectre.Console.Testing;

namespace SelfPlusPlusCLI.Tests.Integration;

[TestFixture]
public sealed class CommandAppIntegrationTests
{
    [Test]
    public void AddConsumptionAndShowJson_FlowsThroughCli()
    {
        using var provider = new TempLogDataPathProvider();

        var addConsole = new TestConsole();
        var addTester = CreateTester(provider, addConsole);

        var addResult = addTester.Run(new[] { "add", "consumption", "substance", "Coffee", "1", "cup" });

        Assert.That(addResult.ExitCode, Is.EqualTo(0), () => addConsole.Output);
        Assert.That(File.Exists(provider.FilePath), Is.True);

        var showConsole = new TestConsole();
        var showTester = CreateTester(provider, showConsole);

        var showResult = showTester.Run(new[] { "show", "--format", "json" });

        Assert.That(showResult.ExitCode, Is.EqualTo(0), () => showConsole.Output);
        Assert.That(showConsole.Output, Does.Contain("Coffee"));
    }

    private static CommandAppTester CreateTester(ILogDataPathProvider provider, TestConsole console)
    {
        var hostBuilder = Host.CreateDefaultBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton(provider);
            services.AddSingleton<IAnsiConsole>(console);
            services.AddSingleton<LogDataService>();
        });

        var registrar = new TypeRegistrar(hostBuilder);
        var settings = new CommandAppTesterSettings
        {
            TrimConsoleOutput = true
        };
        var tester = new CommandAppTester(registrar, settings, console);

        tester.Configure(config =>
        {
            config.AddBranch<AddSettings>("add", add =>
            {
                add.AddCommand<ConsumptionCommand>("consumption");
                add.AddCommand<MeasurementCommand>("measurement");
                add.AddCommand<NoteCommand>("note");
            });

            config.AddCommand<ShowCommand>("show");
        });

        return tester;
    }

    [Test]
    public void AddNoteAndShowTable_FlowsThroughCli()
    {
        using var provider = new TempLogDataPathProvider();

        var addConsole = new TestConsole();
        var addTester = CreateTester(provider, addConsole);

        var addResult = addTester.Run(new[] { "add", "note", "Journal", "Captured insights after journaling" });

        Assert.That(addResult.ExitCode, Is.EqualTo(0), () => addConsole.Output);
        Assert.That(File.Exists(provider.FilePath), Is.True);

        var showConsole = new TestConsole();
        showConsole.Width(200);
        var showTester = CreateTester(provider, showConsole);

        var showResult = showTester.Run(new[] { "show" });

        Assert.That(showResult.ExitCode, Is.EqualTo(0), () => showConsole.Output);
        Assert.That(showConsole.Output, Does.Contain("Note"));
        Assert.That(showConsole.Output, Does.Contain("Captured insights after journaling"));
    }
}

