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
}
