using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SelfPlusPlusCLI.Common;
using SelfPlusPlusCLI.Show;
using SelfPlusPlusCLI.Tests.Support;
using Spectre.Console.Testing;

namespace SelfPlusPlusCLI.Tests.Show;

[TestFixture]
public sealed class ShowCommandTests
{
    [Test]
    public void Execute_WithShowPath_WritesLogFilePath()
    {
        using var provider = new TempLogDataPathProvider();
        var service = new LogDataService(NullLogger<LogDataService>.Instance, provider);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var console = new TestConsole();
        console.Width(200);
        var command = new ShowCommand(configuration, service, console);
        var settings = new ShowSettings { ShowPath = true };

        var exitCode = command.Execute(null!, settings);

        Assert.That(exitCode, Is.EqualTo(0));
        var output = console.Output.Replace(Environment.NewLine, string.Empty);
        Assert.That(output, Is.EqualTo(provider.FilePath));
    }

    [Test]
    public void Execute_WithJsonFormat_WritesJsonArray()
    {
        using var provider = new TempLogDataPathProvider();
        var service = new LogDataService(NullLogger<LogDataService>.Instance, provider);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var console = new TestConsole();
        console.EmitAnsiSequences();
        console.Width(200);
        var command = new ShowCommand(configuration, service, console);

        service.AddLogEntry(JObject.FromObject(new
        {
            Type = "Consumption",
            Name = "Coffee"
        }));

        var settings = new ShowSettings { Format = Format.JSON };

        var exitCode = command.Execute(null!, settings);

        Assert.That(exitCode, Is.EqualTo(0));
        var storedEntries = service.ReadLogEntries();
        Assert.That(storedEntries, Has.Count.EqualTo(1));
        Assert.That(storedEntries[0]["Name"]?.ToString(), Is.EqualTo("Coffee"));
        Assert.That(console.Output.Length, Is.GreaterThan(0));
    }

    [Test]
    public void Execute_WithNoOptions_ShowsOnlyTodaysEntries()
    {
        using var provider = new TempLogDataPathProvider();
        var service = new LogDataService(NullLogger<LogDataService>.Instance, provider);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var console = new TestConsole();
        console.Width(200);
        var command = new ShowCommand(configuration, service, console);

        var now = DateTimeOffset.Now;
        var yesterday = now.AddDays(-1);

        service.AddLogEntry(JObject.FromObject(new
        {
            Type = "Consumption",
            Name = "Today Entry",
            Timestamp = now.ToString("o"),
            Amount = 1,
            Unit = "cup"
        }));

        service.AddLogEntry(JObject.FromObject(new
        {
            Type = "Consumption",
            Name = "Yesterday Entry",
            Timestamp = yesterday.ToString("o"),
            Amount = 2,
            Unit = "cups"
        }));

        var settings = new ShowSettings();

        var exitCode = command.Execute(null!, settings);

        Assert.That(exitCode, Is.EqualTo(0));
        var output = console.Output.NormalizeLineEndings();
        Assert.That(output, Does.Contain("Today Entry"));
        Assert.That(output, Does.Not.Contain("Yesterday Entry"));
    }

    [Test]
    public void Execute_WithStartAndEndDateTime_FiltersEntriesWithinRange()
    {
        using var provider = new TempLogDataPathProvider();
        var service = new LogDataService(NullLogger<LogDataService>.Instance, provider);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var console = new TestConsole();
        console.Width(200);
        var command = new ShowCommand(configuration, service, console);

        var localNow = DateTime.Now;
        var day = localNow.Date;
        var offset = TimeZoneInfo.Local.GetUtcOffset(day);

        service.AddLogEntry(JObject.FromObject(new
        {
            Type = "Consumption",
            Name = "Before Window",
            Timestamp = new DateTimeOffset(day.AddHours(7), offset).ToString("o"),
            Amount = 1,
            Unit = "cup"
        }));

        service.AddLogEntry(JObject.FromObject(new
        {
            Type = "Consumption",
            Name = "Inside Window",
            Timestamp = new DateTimeOffset(day.AddHours(9), offset).ToString("o"),
            Amount = 1,
            Unit = "cup"
        }));

        service.AddLogEntry(JObject.FromObject(new
        {
            Type = "Consumption",
            Name = "After Window",
            Timestamp = new DateTimeOffset(day.AddHours(11), offset).ToString("o"),
            Amount = 1,
            Unit = "cup"
        }));

        var settings = new ShowSettings
        {
            StartDate = day.ToString("yyyy-MM-dd"),
            StartTime = "08:00",
            EndDate = day.ToString("yyyy-MM-dd"),
            EndTime = "10:00"
        };

        var exitCode = command.Execute(null!, settings);

        Assert.That(exitCode, Is.EqualTo(0));
        var output = console.Output.NormalizeLineEndings();
        Assert.That(output, Does.Contain("Inside Window"));
        Assert.That(output, Does.Not.Contain("Before Window"));
        Assert.That(output, Does.Not.Contain("After Window"));
    }
}

