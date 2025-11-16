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

}