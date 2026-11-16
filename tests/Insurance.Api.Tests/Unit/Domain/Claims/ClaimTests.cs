using Insurance.Api.Domain.Claims;
using Insurance.Api.Domain.Policies;
using Insurance.Api.Tests.TestSupport;

namespace Insurance.Api.Tests.Unit.Domain.Claims;

public class ClaimTests
{
    private static readonly DateOnly Jan1 = new(2026, 1, 1);
    private static readonly DateOnly Dec31 = new(2026, 12, 31);

    private static Policy DraftPolicy() => Policy.Create(Guid.NewGuid(), ProductType.Auto, Jan1, Dec31, 100m).Value;

    private static Policy ActivePolicy() => DraftPolicy().Activate([]).Value;

    private static Claim NewClaim() => Claim.Create(ActivePolicy(), Jan1, 50m).Value;

    [Fact]
    public void Create_WhenValid_ThenReturnsNewClaimWithGivenValues()
    {
        var policy = ActivePolicy();

        var result = Claim.Create(policy, new DateOnly(2026, 6, 15), 250.50m);

        Assert.True(result.IsSuccess);
        var claim = result.Value;
        Assert.NotEqual(Guid.Empty, claim.Id);
        Assert.Equal(policy.Id, claim.PolicyId);
        Assert.Equal(new DateOnly(2026, 6, 15), claim.IncidentDate);
        Assert.Equal(250.50m, claim.AmountRequested);
        Assert.Equal(ClaimStatus.New, claim.Status);
        Assert.Null(claim.DecisionReason);
    }

    [Fact]
    public void Create_WhenPolicyNull_ThenThrows()
    {
        Assert.Throws<ArgumentNullException>(() => Claim.Create(null!, Jan1, 50m));
    }

    [Fact]
    public void Create_WhenPolicyIsDraft_ThenPolicyNotActive()
    {
        var result = Claim.Create(DraftPolicy(), Jan1, 50m);

        Assert.Same(ClaimErrors.PolicyNotActive, result.Error);
    }

    [Theory]
    [InlineData("2026-01-01")]
    [InlineData("2026-12-31")]
    public void Create_WhenIncidentOnPolicyBoundary_ThenSucceeds(string incidentDate)
    {
        var result = Claim.Create(ActivePolicy(), Parse.Date(incidentDate), 50m);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("2025-12-31")]
    [InlineData("2027-01-01")]
    public void Create_WhenIncidentOutsidePolicyPeriod_ThenIncidentOutsidePolicyPeriod(string incidentDate)
    {
        var result = Claim.Create(ActivePolicy(), Parse.Date(incidentDate), 50m);

        Assert.Same(ClaimErrors.IncidentOutsidePolicyPeriod, result.Error);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("1.005")]
    public void Create_WhenAmountInvalid_ThenInvalidAmount(string amount)
    {
        var result = Claim.Create(ActivePolicy(), Jan1, Parse.Amount(amount));

        Assert.Same(ClaimErrors.InvalidAmount, result.Error);
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("1.99")]
    [InlineData("1.990")]
    public void Create_WhenAmountPositiveWithAtMostTwoDecimals_ThenSucceeds(string amount)
    {
        var result = Claim.Create(ActivePolicy(), Jan1, Parse.Amount(amount));

        Assert.True(result.IsSuccess);
        Assert.Equal(Parse.Amount(amount), result.Value.AmountRequested);
    }

    [Fact]
    public void Create_WhenAmountInvalidAndPolicyNotActive_ThenValidationErrorWins()
    {
        var result = Claim.Create(DraftPolicy(), Jan1, 0m);

        Assert.Same(ClaimErrors.InvalidAmount, result.Error);
    }

    [Fact]
    public void Decide_WhenApprovingNewClaim_ThenReturnsApprovedCopy()
    {
        var claim = NewClaim();

        var result = claim.Decide(ClaimDecision.Approve, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(ClaimStatus.Approved, result.Value.Status);
        Assert.Equal(claim.Id, result.Value.Id);
        Assert.Equal(ClaimStatus.New, claim.Status);
    }

    [Fact]
    public void Decide_WhenApprovingWithReason_ThenReasonIgnored()
    {
        var result = NewClaim().Decide(ClaimDecision.Approve, "Looks fine");

        Assert.Equal(ClaimStatus.Approved, result.Value.Status);
        Assert.Null(result.Value.DecisionReason);
    }

    [Fact]
    public void Decide_WhenRejectingWithReason_ThenReturnsRejectedCopyWithTrimmedReason()
    {
        var result = NewClaim().Decide(ClaimDecision.Reject, "  Not covered  ");

        Assert.True(result.IsSuccess);
        Assert.Equal(ClaimStatus.Rejected, result.Value.Status);
        Assert.Equal("Not covered", result.Value.DecisionReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Decide_WhenRejectingWithoutReason_ThenDecisionReasonRequired(string? reason)
    {
        var result = NewClaim().Decide(ClaimDecision.Reject, reason);

        Assert.Same(ClaimErrors.DecisionReasonRequired, result.Error);
    }

    [Fact]
    public void Decide_WhenRejectingWithReasonAtMaxLength_ThenSucceeds()
    {
        var reason = new string('r', Claim.DecisionReasonMaxLength);

        var result = NewClaim().Decide(ClaimDecision.Reject, reason);

        Assert.Equal(reason, result.Value.DecisionReason);
    }

    [Fact]
    public void Decide_WhenRejectingWithReasonOverMaxLengthOnlyBecauseOfWhitespace_ThenSucceeds()
    {
        var result = NewClaim().Decide(ClaimDecision.Reject, $" {new string('r', Claim.DecisionReasonMaxLength)} ");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Decide_WhenRejectingWithReasonOverMaxLength_ThenDecisionReasonTooLong()
    {
        var result = NewClaim().Decide(ClaimDecision.Reject, new string('r', Claim.DecisionReasonMaxLength + 1));

        Assert.Same(ClaimErrors.DecisionReasonTooLong, result.Error);
    }

    [Fact]
    public void Decide_WhenApprovingWithReasonOverMaxLength_ThenReasonIgnored()
    {
        var result = NewClaim().Decide(ClaimDecision.Approve, new string('r', Claim.DecisionReasonMaxLength + 1));

        Assert.Equal(ClaimStatus.Approved, result.Value.Status);
    }

    [Theory]
    [InlineData(ClaimDecision.Approve, ClaimDecision.Approve)]
    [InlineData(ClaimDecision.Approve, ClaimDecision.Reject)]
    [InlineData(ClaimDecision.Reject, ClaimDecision.Approve)]
    [InlineData(ClaimDecision.Reject, ClaimDecision.Reject)]
    public void Decide_WhenAlreadyDecided_ThenAlreadyDecided(ClaimDecision first, ClaimDecision second)
    {
        var decided = NewClaim().Decide(first, "Reason").Value;

        var result = decided.Decide(second, "Reason");

        Assert.Same(ClaimErrors.AlreadyDecided, result.Error);
    }

    [Fact]
    public void Decide_WhenRejectingDecidedClaimWithoutReason_ThenValidationErrorWins()
    {
        var decided = NewClaim().Decide(ClaimDecision.Approve, null).Value;

        var result = decided.Decide(ClaimDecision.Reject, null);

        Assert.Same(ClaimErrors.DecisionReasonRequired, result.Error);
    }

    [Fact]
    public void Decide_WhenDecisionUndefined_ThenInvalidDecision()
    {
        var result = NewClaim().Decide((ClaimDecision)99, "Reason");

        Assert.Same(ClaimErrors.InvalidDecision, result.Error);
    }
}
