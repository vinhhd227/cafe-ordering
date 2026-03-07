using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Web.Extensions;

/// <summary>
/// Deserializes a JSON field as string?, accepting both JSON strings and JSON numbers.
/// When a number is received, converts it to its string representation.
/// Useful for tolerating legacy clients that send SmartEnum integer values instead of names.
/// </summary>
public class NumberOrStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var l) ? l.ToString() : reader.GetDouble().ToString(),
            JsonTokenType.Null   => null,
            JsonTokenType.True   => "true",
            JsonTokenType.False  => "false",
            _                    => throw new JsonException($"Unexpected token type {reader.TokenType} when reading string.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value);
    }
}
