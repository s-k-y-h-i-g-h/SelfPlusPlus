using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SelfPlusPlusCLI.Common;
using SelfPlusPlusCLI.Import;
using SelfPlusPlusCLI.Tests.Support;

namespace SelfPlusPlusCLI.Tests.Import;

[TestFixture]
public class SamsungHealthImporterTests
{
    [Test]
    public void Import_AddsSleepMeasurementsFromCsv()
    {
        using var pathProvider = new TempLogDataPathProvider();
        using var exportDirectory = new TempExportDirectory();

        CreateSleepSummaryCsv(exportDirectory.DirectoryPath);
        CreateSleepStageCsv(exportDirectory.DirectoryPath);

        var logService = new LogDataService(new NullLogger<LogDataService>(), pathProvider);
        var importer = new SamsungHealthImporter();

        var result = importer.Import(exportDirectory.DirectoryPath, logService);

        Assert.That(result.SessionsProcessed, Is.EqualTo(1));
        Assert.That(result.MeasurementsAdded, Is.EqualTo(1));

        var entries = logService.ReadLogEntries();
        Assert.That(entries, Has.Count.EqualTo(2)); // Sleep entry + note entry

        var sleepEntry = entries.First(e => e["Type"]?.ToString() == "Measurement" && e["Category"]?.ToString() == "Sleep");
        Assert.That(sleepEntry["Type"]?.ToString(), Is.EqualTo("Measurement"));
        Assert.That(sleepEntry["Category"]?.ToString(), Is.EqualTo("Sleep"));

        Assert.That(sleepEntry["DurationMinutes"]?.Value<double>(), Is.EqualTo(480).Within(0.01));
        Assert.That(sleepEntry["Score"]?.Value<double>(), Is.EqualTo(85).Within(0.01));
        Assert.That(sleepEntry["Efficiency"]?.Value<double>(), Is.EqualTo(92.5).Within(0.01));

        Assert.That(sleepEntry["RemDuration"]?.Value<double>(), Is.EqualTo(90).Within(0.01));
        Assert.That(sleepEntry["DeepDuration"]?.Value<double>(), Is.EqualTo(165).Within(0.01));
        Assert.That(sleepEntry["LightDuration"]?.Value<double>(), Is.EqualTo(210).Within(0.01));
        Assert.That(sleepEntry["AwakeDuration"]?.Value<double>(), Is.EqualTo(15).Within(0.01));

        Assert.That(sleepEntry["PhysicalRecovery"]?.Value<double>(), Is.EqualTo(8.5).Within(0.01));
        Assert.That(sleepEntry["MentalRecovery"]?.Value<double>(), Is.EqualTo(7.0).Within(0.01));

        var timestampToken = sleepEntry["Timestamp"];
        Assert.That(timestampToken, Is.Not.Null);

        var parsedTimestamp = timestampToken!.Type switch
        {
            JTokenType.Date => ConvertDateTime(timestampToken.Value<DateTime>()),
            _ => ConvertDateTime(DateTime.Parse(timestampToken.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal))
        };

        Assert.That(parsedTimestamp, Is.EqualTo(new DateTimeOffset(2025, 3, 2, 6, 30, 0, TimeSpan.Zero)));
    }

    [Test]
    public void Import_DoesNotDuplicateExistingMeasurements()
    {
        using var pathProvider = new TempLogDataPathProvider();
        using var exportDirectory = new TempExportDirectory();

        CreateSleepSummaryCsv(exportDirectory.DirectoryPath);
        CreateSleepStageCsv(exportDirectory.DirectoryPath);

        var logService = new LogDataService(new NullLogger<LogDataService>(), pathProvider);
        var importer = new SamsungHealthImporter();

        var firstResult = importer.Import(exportDirectory.DirectoryPath, logService);
        var secondResult = importer.Import(exportDirectory.DirectoryPath, logService);

        Assert.That(firstResult.MeasurementsAdded, Is.EqualTo(1));
        Assert.That(secondResult.MeasurementsAdded, Is.EqualTo(0));

        var entries = logService.ReadLogEntries();
        Assert.That(entries.Count, Is.EqualTo(2), "Sleep session should only be logged once per source record."); // Sleep entry + note entry
    }

    private static DateTimeOffset ConvertDateTime(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(value, TimeSpan.Zero),
            DateTimeKind.Local => new DateTimeOffset(value.ToUniversalTime(), TimeSpan.Zero),
            _ => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeSpan.Zero)
        };
    }

    private static void CreateSleepSummaryCsv(string directory)
    {
        var path = Path.Combine(directory, "com.samsung.shealth.sleep.20250101000000.csv");
        Directory.CreateDirectory(directory);

        var lines = new[]
        {
            "com.samsung.shealth.sleep,0,0",
            "sleep_score,efficiency,sleep_duration,total_light_duration,total_rem_duration,wake_score,physical_recovery,mental_recovery,quality,com.samsung.health.sleep.start_time,com.samsung.health.sleep.end_time,com.samsung.health.sleep.update_time,com.samsung.health.sleep.create_time,com.samsung.health.sleep.time_offset,com.samsung.health.sleep.datauuid",
            "85,92.5,480,210,90,70,8.5,7.0,4,2025-03-01 22:30:00.000,2025-03-02 06:30:00.000,2025-03-02 07:00:00.000,2025-03-01 23:00:00.000,UTC+0000,abc123-sleep"
        };

        File.WriteAllLines(path, lines);

        var combinedPath = Path.Combine(directory, "com.samsung.shealth.sleep_combined.20250101000500.csv");
        var combinedLines = new[]
        {
            "com.samsung.shealth.sleep_combined,0,0",
            "placeholder",
            "placeholder"
        };

        File.WriteAllLines(combinedPath, combinedLines);

        var baseTime = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(path, baseTime.AddMinutes(-5));
        File.SetLastWriteTimeUtc(combinedPath, baseTime);
    }

    private static void CreateSleepStageCsv(string directory)
    {
        var path = Path.Combine(directory, "com.samsung.health.sleep_stage.20250101000000.csv");
        Directory.CreateDirectory(directory);

        var lines = new[]
        {
            "com.samsung.health.sleep_stage,0,0",
            "start_time,sleep_id,stage,time_offset,end_time",
            "2025-03-01 22:30:00.000,abc123-sleep,40001,UTC+0000,2025-03-01 22:45:00.000",
            "2025-03-01 22:45:00.000,abc123-sleep,40002,UTC+0000,2025-03-02 02:15:00.000",
            "2025-03-02 02:15:00.000,abc123-sleep,40003,UTC+0000,2025-03-02 03:45:00.000",
            "2025-03-02 03:45:00.000,abc123-sleep,40004,UTC+0000,2025-03-02 06:30:00.000"
        };

        File.WriteAllLines(path, lines);
    }

    private sealed class TempExportDirectory : IDisposable
    {
        public TempExportDirectory()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), "SelfPlusPlusTests", "SamsungHealth", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            try
            {
            }
            catch
            {
                // Ignore cleanup issues in tests.
            }
        }
    }
}

