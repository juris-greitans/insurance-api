using System.Text.Json;
using System.Text.Json.Serialization;

namespace Insurance.Api.Controllers.Json;

/// <summary>Creates a <see cref="StrictEnumConverter{TEnum}"/> for every enum type.</summary>
public sealed class StrictEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        return typeToConvert.IsEnum;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(StrictEnumConverter<>).MakeGenericType(typeToConvert))!;
}
