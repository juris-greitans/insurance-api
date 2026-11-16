using System.Text.Json;
using Insurance.Api.Controllers.Json;
using Insurance.Api.Domain.Claims;

namespace Insurance.Api.Tests.Unit.Controllers.Json;

public class StrictEnumConverterTests
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new StrictEnumConverterFactory() } };

    [Theory]
    [InlineData("\"Approve\"", ClaimDecision.Approve)]
    [InlineData("\"Reject\"", ClaimDecision.Reject)]
    public void Read_WhenExactMemberName_ThenReturnsMember(string json, ClaimDecision expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<ClaimDecision>(json, Options));
    }

    [Theory]
    [InlineData("\"approve\"")]
    [InlineData("\"REJECT\"")]
    [InlineData("\" Approve\"")]
    [InlineData("\"Approve \"")]
    [InlineData("\"Approve, Reject\"")]
    [InlineData("\"Approve,Reject\"")]
    [InlineData("\"Maybe\"")]
    [InlineData("\"\"")]
    [InlineData("\"1\"")]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("{}")]
    public void Read_WhenNotAnExactMemberName_ThenThrows(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClaimDecision>(json, Options));
    }

    [Fact]
    public void Read_WhenNullableTargetAndNull_ThenReturnsNull()
    {
        Assert.Null(JsonSerializer.Deserialize<ClaimDecision?>("null", Options));
    }

    [Fact]
    public void Read_WhenNullableTargetAndCombinedNames_ThenThrows()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClaimDecision?>("\"Approve, Reject\"", Options));
    }

    [Fact]
    public void Write_WhenMember_ThenWritesName()
    {
        Assert.Equal("\"Reject\"", JsonSerializer.Serialize(ClaimDecision.Reject, Options));
    }
}
