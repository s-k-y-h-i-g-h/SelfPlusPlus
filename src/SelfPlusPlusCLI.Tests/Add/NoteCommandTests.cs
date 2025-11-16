using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SelfPlusPlusCLI.Add;
using SelfPlusPlusCLI.Common;
using SelfPlusPlusCLI.Tests.Support;
using Spectre.Console.Testing;

namespace SelfPlusPlusCLI.Tests.Add;

[TestFixture]
public sealed class NoteCommandTests
{
    [Test]
    public void Execute_AddsNoteEntry()
    {
        using var provider = new TempLogDataPathProvider();
        var logDataService = new LogDataService(NullLogger<LogDataService>.Instance, provider);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var console = new TestConsole();
        var command = new NoteCommand(configuration, logDataService, console);
        var settings = new NoteSettings
        {
            Category = "Journal",
            Content = "Reflected on training session"
        };

        var exitCode = command.Execute(null!, settings);

        Assert.That(exitCode, Is.EqualTo(0));

        var entries = logDataService.ReadLogEntries();
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0]["Type"]?.ToString(), Is.EqualTo("Note"));
        Assert.That(entries[0]["Category"]?.ToString(), Is.EqualTo("Journal"));
        Assert.That(entries[0]["Content"]?.ToString(), Is.EqualTo("Reflected on training session"));

        var output = console.Output.NormalizeLineEndings();
        Assert.That(output, Does.Contain("Added note entry"));
    }

    [Test]
    public void Validate_ReturnsError_WhenContentIsEmpty()
    {
        var settings = new NoteSettings
        {
            Category = "Journal",
            Content = "   "
        };

        var result = settings.Validate();

        Assert.That(result.Successful, Is.False);
        Assert.That(result.Message, Does.Contain("must not be empty"));
    }

    [Test]
    public void Validate_ReturnsError_WhenCategoryIsEmpty()
    {
        var settings = new NoteSettings
        {
            Category = "   ",
            Content = "Documented progress"
        };

        var result = settings.Validate();

        Assert.That(result.Successful, Is.False);
        Assert.That(result.Message, Does.Contain("Category"));
    }
}


