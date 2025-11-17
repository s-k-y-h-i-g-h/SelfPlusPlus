using System.Collections.Generic;
using System.Text.Json.Serialization;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Add;

sealed class NoteLogEntry : BaseLogEntry
{
    public NoteLogEntry()
    {
        Type = "Note";
    }

    [JsonPropertyName("Content")]
    public string Content { get; set; } = string.Empty;

    public override IEnumerable<string> GetDisplaySegments(DisplayContext context)
    {
        var segments = new List<string>();
        context.AddIfNotNull(segments, context.BuildLabeledValue("Content", Content));
        return segments;
    }
}
