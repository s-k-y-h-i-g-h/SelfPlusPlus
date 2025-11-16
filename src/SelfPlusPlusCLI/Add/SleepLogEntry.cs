using System.Text.Json.Serialization;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Add;

sealed class SleepLogEntry : BaseLogEntry
{
    public SleepLogEntry()
    {
        Type = "Measurement";
        Category = "Sleep";
    }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "Sleep Session";

    [JsonPropertyName("Start")]
    public string? Start { get; set; }

    [JsonPropertyName("End")]
    public string? End { get; set; }

    [JsonPropertyName("DurationMinutes")]
    public double? DurationMinutes { get; set; }

    [JsonPropertyName("Score")]
    public double? Score { get; set; }

    [JsonPropertyName("WakeScore")]
    public double? WakeScore { get; set; }

    [JsonPropertyName("Efficiency")]
    public double? Efficiency { get; set; }

    [JsonPropertyName("Quality")]
    public double? Quality { get; set; }

    [JsonPropertyName("StageMinutes")]
    public SleepStageDurations StageMinutes { get; set; } = new();

    [JsonPropertyName("RecoveryScores")]
    public SleepRecoveryScores RecoveryScores { get; set; } = new();

    [JsonPropertyName("Source")]
    public string? Source { get; set; }

    [JsonPropertyName("SourceId")]
    public string? SourceId { get; set; }

    [JsonPropertyName("Notes")]
    public string? Notes { get; set; }
}

sealed class SleepStageDurations
{
    [JsonPropertyName("Awake")]
    public double? Awake { get; set; }

    [JsonPropertyName("Rem")]
    public double? Rem { get; set; }

    [JsonPropertyName("Light")]
    public double? Light { get; set; }

    [JsonPropertyName("Deep")]
    public double? Deep { get; set; }

    [JsonPropertyName("Unmapped")]
    public double? Unmapped { get; set; }
}

sealed class SleepRecoveryScores
{
    [JsonPropertyName("Mental")]
    public double? Mental { get; set; }

    [JsonPropertyName("Physical")]
    public double? Physical { get; set; }
}