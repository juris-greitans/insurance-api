using Insurance.Api.Domain.Policies;
using Insurance.Api.Tests.TestSupport;

namespace Insurance.Api.Tests.Unit.Domain.Policies;

public class PolicyTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly DateOnly Jan1 = new(2026, 1, 1);
    private static readonly DateOnly Dec31 = new(2026, 12, 31);

    private static Policy Draft(
        DateOnly start,
        DateOnly end,
        ProductType productType = ProductType.Auto,
        Guid? customerId = null) =>
        Policy.Create(customerId ?? CustomerId, productType, start, end, 100m).Value;

    private static Policy Active(
        DateOnly start,
        DateOnly end,
        ProductType productType = ProductType.Auto,
        Guid? customerId = null) =>
        Draft(start, end, productType, customerId).Activate([]).Value;

    [Fact]
    public void Create_WhenValid_ThenReturnsDraftWithGivenValues()
    {
        var result = Policy.Create(CustomerId, ProductType.Travel, Jan1, Dec31, 199.99m);

        Assert.True(result.IsSuccess);
        var policy = result.Value;
        Assert.NotEqual(Guid.Empty, policy.Id);
        Assert.Equal(CustomerId, policy.CustomerId);
        Assert.Equal(ProductType.Travel, policy.ProductType);
        Assert.Equal(Jan1, policy.StartDate);
        Assert.Equal(Dec31, policy.EndDate);
        Assert.Equal(199.99m, policy.Premium);
        Assert.Equal(PolicyStatus.Draft, policy.Status);
    }

    [Fact]
    public void Create_WhenCustomerIdEmpty_ThenCustomerIdRequired()
    {
        var result = Policy.Create(Guid.Empty, ProductType.Auto, Jan1, Dec31, 100m);

        Assert.Same(PolicyErrors.CustomerIdRequired, result.Error);
    }

    [Fact]
    public void Create_WhenProductTypeUndefined_ThenInvalidProductType()
    {
        var result = Policy.Create(CustomerId, (ProductType)99, Jan1, Dec31, 100m);

        Assert.Same(PolicyErrors.InvalidProductType, result.Error);
    }

    [Fact]
    public void Create_WhenEndDateIsDayAfterStartDate_ThenSucceeds()
    {
        var result = Policy.Create(CustomerId, ProductType.Auto, Jan1, Jan1.AddDays(1), 100m);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_WhenEndDateEqualsStartDate_ThenInvalidPeriod()
    {
        var result = Policy.Create(CustomerId, ProductType.Auto, Jan1, Jan1, 100m);

        Assert.Same(PolicyErrors.InvalidPeriod, result.Error);
    }

    [Fact]
    public void Create_WhenEndDateBeforeStartDate_ThenInvalidPeriod()
    {
        var result = Policy.Create(CustomerId, ProductType.Auto, Dec31, Jan1, 100m);

        Assert.Same(PolicyErrors.InvalidPeriod, result.Error);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("10.001")]
    public void Create_WhenPremiumInvalid_ThenInvalidPremium(string premium)
    {
        var result = Policy.Create(CustomerId, ProductType.Auto, Jan1, Dec31, Parse.Amount(premium));

        Assert.Same(PolicyErrors.InvalidPremium, result.Error);
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("10.99")]
    [InlineData("10.990")]
    public void Create_WhenPremiumPositiveWithAtMostTwoDecimals_ThenSucceeds(string premium)
    {
        var result = Policy.Create(CustomerId, ProductType.Auto, Jan1, Dec31, Parse.Amount(premium));

        Assert.True(result.IsSuccess);
        Assert.Equal(Parse.Amount(premium), result.Value.Premium);
    }

    [Fact]
    public void Activate_WhenDraftAndNoOtherPolicies_ThenReturnsActiveCopy()
    {
        var draft = Draft(Jan1, Dec31);

        var result = draft.Activate([draft]);

        Assert.True(result.IsSuccess);
        Assert.Equal(PolicyStatus.Active, result.Value.Status);
        Assert.Equal(draft.Id, result.Value.Id);
        Assert.Equal(PolicyStatus.Draft, draft.Status);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ThenNotDraft()
    {
        var active = Active(Jan1, Dec31);

        var result = active.Activate([active]);

        Assert.Same(PolicyErrors.NotDraft, result.Error);
    }

    [Fact]
    public void Activate_WhenPoliciesNull_ThenThrows()
    {
        Assert.Throws<ArgumentNullException>(() => Draft(Jan1, Dec31).Activate(null!));
    }

    [Theory]
    [InlineData("2026-01-01", "2026-12-31", "2026-01-01", "2026-12-31")] // identical
    [InlineData("2026-01-01", "2026-06-30", "2026-06-30", "2026-12-31")] // existing ends on the day new starts
    [InlineData("2026-06-30", "2026-12-31", "2026-01-01", "2026-06-30")] // new ends on the day existing starts
    [InlineData("2026-01-01", "2026-12-31", "2026-03-01", "2026-04-01")] // new inside existing
    [InlineData("2026-03-01", "2026-04-01", "2026-01-01", "2026-12-31")] // existing inside new
    public void Activate_WhenActivePolicyOfSameTypeOverlaps_ThenOverlappingActivePolicy(
        string existingStart, string existingEnd, string newStart, string newEnd)
    {
        var existing = Active(Parse.Date(existingStart), Parse.Date(existingEnd));
        var draft = Draft(Parse.Date(newStart), Parse.Date(newEnd));

        var result = draft.Activate([existing, draft]);

        Assert.Same(PolicyErrors.OverlappingActivePolicy, result.Error);
    }

    [Theory]
    [InlineData("2026-01-01", "2026-06-29", "2026-06-30", "2026-12-31")] // existing ends the day before new starts
    [InlineData("2026-07-01", "2026-12-31", "2026-01-01", "2026-06-30")] // new ends the day before existing starts
    public void Activate_WhenActivePolicyOfSameTypeAdjacentButNotOverlapping_ThenSucceeds(
        string existingStart, string existingEnd, string newStart, string newEnd)
    {
        var existing = Active(Parse.Date(existingStart), Parse.Date(existingEnd));
        var draft = Draft(Parse.Date(newStart), Parse.Date(newEnd));

        var result = draft.Activate([existing]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Activate_WhenOverlappingPolicyIsDraft_ThenSucceeds()
    {
        var otherDraft = Draft(Jan1, Dec31);
        var draft = Draft(Jan1, Dec31);

        var result = draft.Activate([otherDraft]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Activate_WhenOverlappingActivePolicyHasDifferentProductType_ThenSucceeds()
    {
        var existing = Active(Jan1, Dec31, ProductType.Property);
        var draft = Draft(Jan1, Dec31, ProductType.Auto);

        var result = draft.Activate([existing]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Activate_WhenOverlappingActivePolicyBelongsToAnotherCustomer_ThenSucceeds()
    {
        var existing = Active(Jan1, Dec31, customerId: Guid.NewGuid());
        var draft = Draft(Jan1, Dec31);

        var result = draft.Activate([existing]);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("2026-01-01", true)]
    [InlineData("2026-12-31", true)]
    [InlineData("2026-06-15", true)]
    [InlineData("2025-12-31", false)]
    [InlineData("2027-01-01", false)]
    public void Covers_WhenDateGiven_ThenTrueOnlyWithinInclusivePeriod(string date, bool expected)
    {
        var policy = Draft(Jan1, Dec31);

        Assert.Equal(expected, policy.Covers(Parse.Date(date)));
    }
}
