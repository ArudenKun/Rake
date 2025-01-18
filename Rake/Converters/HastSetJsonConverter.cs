using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rake.Converters;

public class HastSetJsonConverter<T, TConverter> : JsonConverter<HashSet<T>>
    where TConverter : JsonConverter<T>, new()
{
    private readonly JsonConverter<T> _valueConverter = new TConverter();

    public override HashSet<T> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType is not JsonTokenType.StartArray)
        {
            reader.Skip();
            return [];
        }

        var hashSet = new HashSet<T>();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            var item = _valueConverter.Read(ref reader, typeof(T), options);
            if (item is not null)
            {
                hashSet.Add(item);
            }
        }

        return hashSet;
    }

    public override void Write(
        Utf8JsonWriter writer,
        HashSet<T> value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartArray();

        foreach (var item in value)
        {
            _valueConverter.Write(writer, item, options);
        }

        writer.WriteEndArray();
    }
}
