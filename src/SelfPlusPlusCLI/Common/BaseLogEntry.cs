using System;
using System.Text.Json.Serialization;

namespace SelfPlusPlusCLI.Common;

public abstract class BaseLogEntry
{
    [JsonPropertyName("Timestamp")]
    public string Timestamp { get; set; } = DateTimeOffset.UtcNow.ToString("o");

    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("Category")]
    public string Category { get; set; } = string.Empty;
}