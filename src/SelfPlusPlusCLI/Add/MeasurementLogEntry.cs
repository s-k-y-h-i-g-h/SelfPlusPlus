using System.Text.Json.Serialization;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Add;

sealed class MeasurementLogEntry : BaseLogEntry
{
    [JsonPropertyName("Category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("Type")]
    public string Type { get; set; } = "Measurement";

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Value")]
    public double Value { get; set; } = 0.0;

    [JsonPropertyName("Unit")]
    public string Unit { get; set; } = string.Empty;
}

