using System.Text.Json.Serialization;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Add;

sealed class ConsumptionLogEntry : BaseLogEntry
{
    public ConsumptionLogEntry()
    {
        Type = "Consumption";
    }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Amount")]
    public double? Amount { get; set; }

    [JsonPropertyName("Unit")]
    public string? Unit { get; set; }
}