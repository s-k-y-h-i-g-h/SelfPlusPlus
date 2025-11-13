using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SelfPlusPlusCLI.Add;
using SelfPlusPlusCLI.Common;
using SelfPlusPlusCLI.Tests.Support;
using Spectre.Console.Testing;

namespace SelfPlusPlusCLI.Tests.Add;

[TestFixture]
public sealed class MeasurementCommandTests
{
    [Test]
    public void Execute_AddsMeasurementEntry()
    {
        using var provider = new TempLogDataPathProvider();
        var logDataService = new LogDataService(NullLogger<LogDataService>.Instance, provider);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var console = new TestConsole();
        var command = new MeasurementCommand(configuration, logDataService, console);
        var settings = new MeasurementSettings
        {
            Category = MeasurementCategory.Vitals,
            Name = "Heart Rate",
            Value = 70,
            Unit = "bpm"
        };

        var exitCode = command.Execute(null!, settings);

        Assert.That(exitCode, Is.EqualTo(0));

        var entries = logDataService.ReadLogEntries();
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0]["Type"]?.ToString(), Is.EqualTo("Measurement"));
        Assert.That(entries[0]["Name"]?.ToString(), Is.EqualTo("Heart Rate"));

        var output = console.Output.NormalizeLineEndings();
        Assert.That(output, Does.Contain("Added measurement entry"));
    }

    [Test]
    public void Execute_AllowsSubjectiveMeasurementWithoutUnit()
    {
        using var provider = new TempLogDataPathProvider();
        var logDataService = new LogDataService(NullLogger<LogDataService>.Instance, provider);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var console = new TestConsole();
        var command = new MeasurementCommand(configuration, logDataService, console);
        var settings = new MeasurementSettings
        {
            Category = MeasurementCategory.Subjective,
            Name = "Mood",
            Value = 7
        };

        var exitCode = command.Execute(null!, settings);

        Assert.That(exitCode, Is.EqualTo(0));

        var entries = logDataService.ReadLogEntries();
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0]["Unit"]?.ToString(), Is.EqualTo(string.Empty));

        var output = console.Output.NormalizeLineEndings();
        Assert.That(output, Does.Contain("Subjective Mood 7"));
    }

    [Test]
    public void Validate_ReturnsError_WhenUnitMissingForVitals()
    {
        var settings = new MeasurementSettings
        {
            Category = MeasurementCategory.Vitals,
            Name = "Blood Pressure",
            Value = 120
        };

        var result = settings.Validate();

        Assert.That(result.Successful, Is.False);
    }

    [Test]
    public void Validate_Succeeds_WhenUnitMissingForSubjective()
    {
        var settings = new MeasurementSettings
        {
            Category = MeasurementCategory.Subjective,
            Name = "Energy",
            Value = 5
        };

        var result = settings.Validate();

        Assert.That(result.Successful, Is.True);
    }

    [Test]
    public void Validate_ReturnsError_WhenSubjectiveValueOutOfRange()
    {
        var settings = new MeasurementSettings
        {
            Category = MeasurementCategory.Subjective,
            Name = "Mood",
            Value = 11
        };

        var result = settings.Validate();

        Assert.That(result.Successful, Is.False);
        Assert.That(result.Message, Does.Contain("between 0 and 10"));
    }

    [Test]
    public void Validate_ReturnsError_WhenSubjectiveValueNotInteger()
    {
        var settings = new MeasurementSettings
        {
            Category = MeasurementCategory.Subjective,
            Name = "Mood",
            Value = 7.5f
        };

        var result = settings.Validate();

        Assert.That(result.Successful, Is.False);
        Assert.That(result.Message, Does.Contain("integer"));
    }
}

