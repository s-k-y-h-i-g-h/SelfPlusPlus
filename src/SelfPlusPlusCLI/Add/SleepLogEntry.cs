using System.Collections.Generic;
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


    [JsonPropertyName("DurationMinutes")]
    public double? DurationMinutes { get; set; }

    [JsonPropertyName("Score")]
    public double? Score { get; set; }

    [JsonPropertyName("WakeScore")]
    public double? WakeScore { get; set; }

    [JsonPropertyName("Efficiency")]
    public double? Efficiency { get; set; }

    [JsonPropertyName("AwakeDurationMinutes")]
    public double? AwakeDurationMinutes { get; set; }

    [JsonPropertyName("REMDurationMinutes")]
    public double? REMDurationMinutes { get; set; }

    [JsonPropertyName("LightDurationMinutes")]
    public double? LightDurationMinutes { get; set; }

    [JsonPropertyName("DeepDurationMinutes")]
    public double? DeepDurationMinutes { get; set; }

    [JsonPropertyName("MentalRecovery")]
    public double? MentalRecovery { get; set; }

    [JsonPropertyName("PhysicalRecovery")]
    public double? PhysicalRecovery { get; set; }

    public override IEnumerable<string> GetDisplaySegments(DisplayContext context)
    {
        var segments = new List<string>();

        // Duration (if available)
        context.AddIfNotNull(segments, context.BuildLabeledValue("Duration", context.FormatDurationMinutes(DurationMinutes)));

        // Scores
        context.AddIfNotNull(segments, context.BuildLabeledValue("Score", Score));
        context.AddIfNotNull(segments, context.BuildLabeledValue("Efficiency", Efficiency, "%"));
        context.AddIfNotNull(segments, context.BuildLabeledValue("Wake Score", WakeScore));

        // Recovery scores
        context.AddIfNotNull(segments, context.BuildLabeledValue("Mental Recovery", MentalRecovery));
        context.AddIfNotNull(segments, context.BuildLabeledValue("Physical Recovery", PhysicalRecovery));

        // Sleep stages
        context.AddIfNotNull(segments, context.BuildLabeledValue("Awake", context.FormatDurationMinutes(AwakeDurationMinutes)));
        context.AddIfNotNull(segments, context.BuildLabeledValue("REM", context.FormatDurationMinutes(REMDurationMinutes)));
        context.AddIfNotNull(segments, context.BuildLabeledValue("Light", context.FormatDurationMinutes(LightDurationMinutes)));
        context.AddIfNotNull(segments, context.BuildLabeledValue("Deep", context.FormatDurationMinutes(DeepDurationMinutes)));

        return segments;
    }
}