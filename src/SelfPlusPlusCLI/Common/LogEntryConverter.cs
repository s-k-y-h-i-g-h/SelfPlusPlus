using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SelfPlusPlusCLI.Add;

namespace SelfPlusPlusCLI.Common;

/// <summary>
/// Custom JSON converter that uses type discriminators for automatic deserialization
/// </summary>
public class LogEntryConverter : JsonConverter<BaseLogEntry>
{
    private const string TypeDiscriminator = "$type";

    public override bool CanWrite => false;

    public static BaseLogEntry? Deserialize(JObject jsonObject)
    {
        var serializer = new JsonSerializer();
        serializer.Converters.Add(new LogEntryConverter());

        using var reader = jsonObject.CreateReader();
        return serializer.Deserialize<BaseLogEntry>(reader);
    } // We don't use this for serialization

    public override void WriteJson(JsonWriter writer, BaseLogEntry? value, JsonSerializer serializer)
    {
        throw new NotImplementedException("Serialization not implemented for this example");
    }

    public override BaseLogEntry? ReadJson(JsonReader reader, Type objectType, BaseLogEntry? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);

        // Require type discriminator field
        var discriminator = jsonObject[TypeDiscriminator]?.ToString();
        if (string.IsNullOrEmpty(discriminator))
        {
            throw new JsonSerializationException($"Missing required {TypeDiscriminator} field");
        }

        // Use discriminator to determine concrete type
        return discriminator switch
        {
            "SleepLogEntry" => jsonObject.ToObject<SleepLogEntry>()!,
            "MeasurementLogEntry" => jsonObject.ToObject<MeasurementLogEntry>()!,
            "ConsumptionLogEntry" => jsonObject.ToObject<ConsumptionLogEntry>()!,
            "NoteLogEntry" => jsonObject.ToObject<NoteLogEntry>()!,
            _ => throw new JsonSerializationException($"Unknown log entry type: {discriminator}")
        };
    }
}
