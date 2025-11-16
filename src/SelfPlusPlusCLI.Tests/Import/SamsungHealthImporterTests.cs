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
        Assert.That(result.MeasurementsAdded, Is.GreaterThanOrEqualTo(6));

        var entries = logService.ReadLogEntries();
        var measurements = entries
            .Where(e => string.Equals(e["Type"]?.ToString(), "Measurement", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.That(measurements, Is.Not.Empty, "Expected measurement entries to be added.");

        var sleepDuration = FindMeasurement(measurements, "Sleep Duration");
        Assert.That(sleepDuration, Is.Not.Null);
        Assert.That(sleepDuration!["Category"]?.ToString(), Is.EqualTo("Sleep"));
        Assert.That(sleepDuration["Unit"]?.ToString(), Is.EqualTo("minutes"));
        Assert.That(sleepDuration["Value"]?.Value<double>(), Is.EqualTo(480).Within(0.01));
        Assert.That(sleepDuration["Content"]?.ToString(), Does.Contain("Start: "));

        var sleepScore = FindMeasurement(measurements, "Sleep Score");
        Assert.That(sleepScore?["Value"]?.Value<double>(), Is.EqualTo(85).Within(0.01));
        Assert.That(sleepScore?["Unit"]?.ToString(), Is.EqualTo("points"));

        var remDuration = FindMeasurement(measurements, "REM Sleep Duration");
        Assert.That(remDuration?["Value"]?.Value<double>(), Is.EqualTo(90).Within(0.01));

        var deepDuration = FindMeasurement(measurements, "Deep Sleep Duration");
        Assert.That(deepDuration?["Value"]?.Value<double>(), Is.EqualTo(165).Within(0.01));

        var timestampRaw = sleepDuration?["Timestamp"]?.ToString();
        Assert.That(timestampRaw, Is.Not.Null);
        var parsedTimestamp = DateTime.Parse(timestampRaw!, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);
        var utcTimestamp = parsedTimestamp.Kind == DateTimeKind.Utc ? parsedTimestamp : parsedTimestamp.ToUniversalTime();
        Assert.That(utcTimestamp, Is.EqualTo(new DateTime(2025, 3, 2, 6, 30, 0, DateTimeKind.Utc)));
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

        Assert.That(firstResult.MeasurementsAdded, Is.GreaterThan(0));
        Assert.That(secondResult.MeasurementsAdded, Is.EqualTo(0));

        var entries = logService.ReadLogEntries();
        var measurements = entries
            .Where(e => string.Equals(e["Type"]?.ToString(), "Measurement", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var sleepDurationEntries = measurements.Where(e => e["Name"]?.ToString() == "Sleep Duration").ToList();
        Assert.That(sleepDurationEntries.Count, Is.EqualTo(1), "Sleep Duration should only be logged once per session.");
    }

    private static JObject? FindMeasurement(IEnumerable<JObject> entries, string name)
    {
        return entries.FirstOrDefault(e => string.Equals(e["Name"]?.ToString(), name, StringComparison.OrdinalIgnoreCase));
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
                if (Directory.Exists(DirectoryPath))
                {
                    Directory.Delete(DirectoryPath, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup issues in tests.
            }
        }
    }
}

