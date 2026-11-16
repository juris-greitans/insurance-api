using System.Text.Json;
using Insurance.Api.Controllers.Json;
using Insurance.Api.Domain.Claims;

namespace Insurance.Api.Tests.Unit.Controllers.Json;

public class StrictEnumConverterFactoryTests
{
    [Fact]
    public void CanConvert_WhenTypeGiven_ThenTrueOnlyForEnums()
    {
        var factory = new StrictEnumConverterFactory();

        Assert.True(factory.CanConvert(typeof(ClaimDecision)));
        Assert.False(factory.CanConvert(typeof(string)));
        Assert.False(factory.CanConvert(typeof(int)));
    }

    [Fact]
    public void CreateConverter_WhenEnumType_ThenReturnsStrictConverterForThatType()
    {
        var converter = new StrictEnumConverterFactory().CreateConverter(typeof(ClaimDecision), JsonSerializerOptions.Default);

        Assert.IsType<StrictEnumConverter<ClaimDecision>>(converter);
    }
}
