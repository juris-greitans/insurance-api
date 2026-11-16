using System.Text.Json;
using System.Text.Json.Serialization;

namespace Insurance.Api.Controllers.Json;

/// <summary>
/// Reads an enum only from its exact member name (case-sensitive). Rejects numbers, other casings, surrounding
/// whitespace and comma-separated combinations, which the built-in <see cref="JsonStringEnumConverter"/> accepts
/// (e.g. "Approve, Reject" would be OR'ed into Reject).
/// </summary>
public sealed class StrictEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private static readonly Dictionary<string, TEnum> ValuesByName =
        Enum.GetValues<TEnum>().ToDictionary(value => value.ToString(), StringComparer.Ordinal);

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String
            && ValuesByName.TryGetValue(reader.GetString()!, out var value))
        {
            return value;
        }

        throw new JsonException($"Expected one of: {string.Join(", ", ValuesByName.Keys)}.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.ToString());
    }
}
