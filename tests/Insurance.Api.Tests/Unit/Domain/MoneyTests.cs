using Insurance.Api.Domain.Common;

namespace Insurance.Api.Tests.Unit.Domain;

public class MoneyTests
{
    [Theory]
    [InlineData("0.01")]
    [InlineData("1")]
    [InlineData("1.5")]
    [InlineData("1.23")]
    [InlineData("1.230")]
    [InlineData("79228162514264337593543950335")]
    public void IsValidPositiveAmount_WhenPositiveWithAtMostTwoDecimals_ThenTrue(string amount)
    {
        Assert.True(Money.IsValidPositiveAmount(Parse.Amount(amount)));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-100")]
    [InlineData("0.001")]
    [InlineData("1.234")]
    public void IsValidPositiveAmount_WhenNotPositiveOrTooManyDecimals_ThenFalse(string amount)
    {
        Assert.False(Money.IsValidPositiveAmount(Parse.Amount(amount)));
    }
}
