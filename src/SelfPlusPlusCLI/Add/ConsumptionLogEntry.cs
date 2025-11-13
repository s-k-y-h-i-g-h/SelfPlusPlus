using System.Text.Json.Serialization;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Add;

sealed class ConsumptionLogEntry : BaseLogEntry
{
    [JsonPropertyName("Type")]
    public string Type { get; set; } = "Consumption";

    [JsonPropertyName("Category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Amount")]
    public double? Amount { get; set; }

    [JsonPropertyName("Unit")]
    public string? Unit { get; set; }
}