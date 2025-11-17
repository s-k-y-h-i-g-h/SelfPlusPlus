using System;
using System.Globalization;
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

        var entry = JObject.FromObject(new
        {
            Type = "Consumption",
            Name = "Coffee"
        });
        entry["$type"] = "ConsumptionLogEntry";
        service.AddLogEntry(entry);

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

        var todayEntry = JObject.FromObject(new
        {
            Type = "Consumption",
            Name = "Today Entry",
            Timestamp = now.ToString("o"),
            Amount = 1,
            Unit = "cup"
        });
        todayEntry["$type"] = "ConsumptionLogEntry";
        service.AddLogEntry(todayEntry);

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

        var beforeEntry = JObject.FromObject(new
        {
            Type = "Consumption",
            Name = "Before Window",
            Timestamp = new DateTimeOffset(day.AddHours(7), offset).ToString("o"),
            Amount = 1,
            Unit = "cup"
        });
        beforeEntry["$type"] = "ConsumptionLogEntry";
        service.AddLogEntry(beforeEntry);

        var insideEntry = JObject.FromObject(new
        {
            Type = "Consumption",
            Name = "Inside Window",
            Timestamp = new DateTimeOffset(day.AddHours(9), offset).ToString("o"),
            Amount = 1,
            Unit = "cup"
        });
        insideEntry["$type"] = "ConsumptionLogEntry";
        service.AddLogEntry(insideEntry);

        var afterEntry = JObject.FromObject(new
        {
            Type = "Consumption",
            Name = "After Window",
            Timestamp = new DateTimeOffset(day.AddHours(11), offset).ToString("o"),
            Amount = 1,
            Unit = "cup"
        });
        afterEntry["$type"] = "ConsumptionLogEntry";
        service.AddLogEntry(afterEntry);

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

    [Test]
    public void Execute_WithCategoryFilter_ReturnsOnlyMatchingEntries()
    {
        using var provider = new TempLogDataPathProvider();
        var service = new LogDataService(NullLogger<LogDataService>.Instance, provider);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var console = new TestConsole();
        console.Width(200);
        var command = new ShowCommand(configuration, service, console);

        var healthEntry = JObject.FromObject(new
        {
            Type = "Measurement",
            Category = "Health",
            Name = "Sleep Duration",
            Value = 420,
            Unit = "minutes",
            Timestamp = DateTimeOffset.UtcNow.ToString("o")
        });
        healthEntry["$type"] = "MeasurementLogEntry";
        service.AddLogEntry(healthEntry);

        var vitalsEntry = JObject.FromObject(new
        {
            Type = "Measurement",
            Category = "Vitals",
            Name = "Heart Rate",
            Value = 60,
            Unit = "bpm",
            Timestamp = DateTimeOffset.UtcNow.ToString("o")
        });
        vitalsEntry["$type"] = "MeasurementLogEntry";
        service.AddLogEntry(vitalsEntry);

        var settings = new ShowSettings
        {
            Category = "Health"
        };

        var exitCode = command.Execute(null!, settings);

        Assert.That(exitCode, Is.EqualTo(0));
        var output = console.Output.NormalizeLineEndings();
        Assert.That(output, Does.Contain("Sleep Duration"));
        Assert.That(output, Does.Not.Contain("Heart Rate"));
    }

    [Test]
    public void Execute_SortsEntriesByTimestampAscending()
    {
        using var provider = new TempLogDataPathProvider();
        var service = new LogDataService(NullLogger<LogDataService>.Instance, provider);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var console = new TestConsole();
        console.Width(200);
        var command = new ShowCommand(configuration, service, console);

        var today = DateTimeOffset.UtcNow.Date;
        var earlierTimestamp = today.AddHours(10).ToString("o", CultureInfo.InvariantCulture); // 10 AM today
        var laterTimestamp = today.AddHours(14).ToString("o", CultureInfo.InvariantCulture); // 2 PM today

        var laterEntry = JObject.FromObject(new
        {
            Type = "Measurement",
            Category = "Health",
            Name = "Later Entry",
            Value = 2,
            Unit = "points",
            Timestamp = laterTimestamp
        });
        laterEntry["$type"] = "MeasurementLogEntry";
        service.AddLogEntry(laterEntry);

        var earlierEntry = JObject.FromObject(new
        {
            Type = "Measurement",
            Category = "Health",
            Name = "Earlier Entry",
            Value = 1,
            Unit = "points",
            Timestamp = earlierTimestamp
        });
        earlierEntry["$type"] = "MeasurementLogEntry";
        service.AddLogEntry(earlierEntry);

        var storedEntries = service.ReadLogEntries();
        Assert.That(storedEntries, Has.Count.EqualTo(2));

        var settings = new ShowSettings
        {
            Category = "Health",
            StartDate = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            EndDate = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Format = Format.JSON
        };

        var exitCode = command.Execute(null!, settings);

        Assert.That(exitCode, Is.EqualTo(0));
        var json = console.Output.Trim();
        var array = JArray.Parse(json);
        Assert.That(array.Count, Is.EqualTo(2));
        Assert.That(array[0]["Name"]?.ToString(), Is.EqualTo("Earlier Entry"));
        Assert.That(array[1]["Name"]?.ToString(), Is.EqualTo("Later Entry"));
    }
}

