using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SelfPlusPlusCLI.Add;
using SelfPlusPlusCLI.Common;
using SelfPlusPlusCLI.Tests.Support;
using Spectre.Console.Testing;

namespace SelfPlusPlusCLI.Tests.Add;

[TestFixture]
public sealed class ConsumptionCommandTests
{
    [Test]
    public void Execute_AddsEntryAndWritesSuccessMarkup()
    {
        using var provider = new TempLogDataPathProvider();
        var logDataService = new LogDataService(NullLogger<LogDataService>.Instance, provider);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var console = new TestConsole();
        var command = new ConsumptionCommand(configuration, logDataService, console);
        var settings = new ConsumptionSettings
        {
            Category = ConsumptionCategory.Substance,
            Name = "Coffee",
            Amount = 1,
            Unit = "cup"
        };

        var exitCode = command.Execute(null!, settings);

        Assert.That(exitCode, Is.EqualTo(0));

        var entries = logDataService.ReadLogEntries();
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0]["Name"]?.ToString(), Is.EqualTo("Coffee"));

        var output = console.Output.NormalizeLineEndings();
        Assert.That(output, Does.Contain("Added consumption entry"));
    }
}

