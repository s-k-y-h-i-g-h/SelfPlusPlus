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

    [JsonPropertyName("AwakeDuration")]
    public double? AwakeDuration { get; set; }

    [JsonPropertyName("RemDuration")]
    public double? RemDuration { get; set; }

    [JsonPropertyName("LightDuration")]
    public double? LightDuration { get; set; }

    [JsonPropertyName("DeepDuration")]
    public double? DeepDuration { get; set; }

    [JsonPropertyName("UnmappedDuration")]
    public double? UnmappedDuration { get; set; }

    [JsonPropertyName("MentalRecovery")]
    public double? MentalRecovery { get; set; }

    [JsonPropertyName("PhysicalRecovery")]
    public double? PhysicalRecovery { get; set; }

}