using System.Collections.Generic;
using System.Text.Json.Serialization;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Add;

sealed class MeasurementLogEntry : BaseLogEntry
{
    public MeasurementLogEntry()
    {
        Type = "Measurement";
    }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Value")]
    public double Value { get; set; }

    [JsonPropertyName("Unit")]
    public string Unit { get; set; } = string.Empty;

    public override IEnumerable<string> GetDisplaySegments(DisplayContext context)
    {
        var segments = new List<string>();

        context.AddIfNotNull(segments, context.BuildLabeledValue(Name, Value, Unit));

        return segments;
    }
}