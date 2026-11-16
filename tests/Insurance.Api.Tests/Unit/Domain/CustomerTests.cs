using Insurance.Api.Domain.Customers;

namespace Insurance.Api.Tests.Unit.Domain;

public class CustomerTests
{
    [Fact]
    public void Create_WhenFullNameValid_ThenReturnsCustomerWithNewId()
    {
        var result = Customer.Create("Jane Doe");

        Assert.True(result.IsSuccess);
        Assert.Equal("Jane Doe", result.Value.FullName);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Fact]
    public void Create_WhenCalledTwice_ThenIdsDiffer()
    {
        var first = Customer.Create("Jane Doe").Value;
        var second = Customer.Create("Jane Doe").Value;

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Create_WhenFullNameHasSurroundingWhitespace_ThenTrimsIt()
    {
        var result = Customer.Create("  Jane Doe  ");

        Assert.Equal("Jane Doe", result.Value.FullName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Create_WhenFullNameMissingOrWhitespace_ThenFullNameRequired(string? fullName)
    {
        var result = Customer.Create(fullName);

        Assert.Same(CustomerErrors.FullNameRequired, result.Error);
    }

    [Fact]
    public void Create_WhenFullNameAtMaxLength_ThenSucceeds()
    {
        var result = Customer.Create(new string('a', Customer.FullNameMaxLength));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_WhenFullNameExceedsMaxLength_ThenFullNameTooLong()
    {
        var result = Customer.Create(new string('a', Customer.FullNameMaxLength + 1));

        Assert.Same(CustomerErrors.FullNameTooLong, result.Error);
    }

    [Fact]
    public void Create_WhenFullNameExceedsMaxLengthOnlyBecauseOfWhitespace_ThenSucceeds()
    {
        var result = Customer.Create($" {new string('a', Customer.FullNameMaxLength)} ");

        Assert.True(result.IsSuccess);
    }
}
