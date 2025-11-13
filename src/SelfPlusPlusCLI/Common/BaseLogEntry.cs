using System;
using System.Text.Json.Serialization;

namespace SelfPlusPlusCLI.Common;

public abstract class BaseLogEntry
{
    [JsonPropertyName("Timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
}

