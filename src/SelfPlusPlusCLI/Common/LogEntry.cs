using System.Text.Json;
using System.Text.Json.Serialization;

namespace SelfPlusPlusCLI.Common;

public class LogEntry
{
    [JsonPropertyName("Timestamp")]
    public string Timestamp { get; set; } = string.Empty;
    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;
    [JsonPropertyName("Category")]
    public string Category { get; set; } = string.Empty;
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Amount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Amount { get; set; }

    [JsonPropertyName("Value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Value { get; set; }

    [JsonPropertyName("Unit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Unit { get; set; }
} 