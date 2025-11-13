using System.Text.Json.Serialization;
using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Add;

sealed class NoteLogEntry : BaseLogEntry
{
    [JsonPropertyName("Type")]
    public string Type { get; set; } = "Note";

    [JsonPropertyName("Content")]
    public string Content { get; set; } = string.Empty;
}


