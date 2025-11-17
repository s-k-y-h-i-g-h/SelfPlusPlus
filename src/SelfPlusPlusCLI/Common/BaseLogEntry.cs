using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SelfPlusPlusCLI.Common;

public interface IDisplayable
{
    IEnumerable<string> GetDisplaySegments(DisplayContext context);
}

[JsonConverter(typeof(LogEntryConverter))]
public abstract class BaseLogEntry : IDisplayable
{
    [JsonPropertyName("Timestamp")]
    public string Timestamp { get; set; } = DateTimeOffset.UtcNow.ToString("o");

    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("Category")]
    public string Category { get; set; } = string.Empty;

    public abstract IEnumerable<string> GetDisplaySegments(DisplayContext context);
}
