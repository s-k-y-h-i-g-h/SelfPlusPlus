using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SelfPlusPlusCLI.Common;
using SelfPlusPlusCLI.Tests.Support;

namespace SelfPlusPlusCLI.Tests.Common;

[TestFixture]
public sealed class LogDataServiceTests
{
    [Test]
    public void ReadLogEntries_ReturnsEmptyList_WhenFileDoesNotExist()
    {
        using var provider = new TempLogDataPathProvider();
        var service = CreateService(provider);

        var result = service.ReadLogEntries();

        Assert.That(result, Is.Empty);
        Assert.That(File.Exists(provider.FilePath), Is.False);
    }

    [Test]
    public void AddLogEntry_PersistsEntryToPathProvided()
    {
        using var provider = new TempLogDataPathProvider();
        var service = CreateService(provider);

        var entry = JObject.FromObject(new
        {
            Type = "Consumption",
            Name = "Coffee"
        });

        service.AddLogEntry(entry);

        Assert.That(File.Exists(provider.FilePath), Is.True);

        var storedEntries = service.ReadLogEntries();

        Assert.That(storedEntries, Has.Count.EqualTo(1));
        Assert.That(storedEntries[0]["Name"]?.ToString(), Is.EqualTo("Coffee"));
    }

    [Test]
    public void GetLogDataFilePath_UsesInjectedProvider()
    {
        using var provider = new TempLogDataPathProvider();
        var service = CreateService(provider);

        var pathFromService = service.GetLogDataFilePath();

        Assert.That(pathFromService, Is.EqualTo(provider.FilePath));
    }

    private static LogDataService CreateService(ILogDataPathProvider provider)
    {
        return new LogDataService(NullLogger<LogDataService>.Instance, provider);
    }
}

