using Insurance.Api.Application;
using Insurance.Api.Application.Abstractions;
using Insurance.Api.Domain.Claims;
using Insurance.Api.Domain.Policies;
using Insurance.Api.Infrastructure;
using Insurance.Api.Tests.TestSupport;
using Moq;

namespace Insurance.Api.Tests.Unit.Application;

public class ClaimServiceTests
{
    private static readonly DateOnly Jan1 = new(2026, 1, 1);
    private static readonly DateOnly Dec31 = new(2026, 12, 31);

    private readonly InMemoryPolicyRepository _policies = new();
    private readonly InMemoryClaimRepository _claims = new();
    private readonly ClaimService _service;
    private readonly Policy _activePolicy;

    public ClaimServiceTests()
    {
        _service = new ClaimService(_policies, _claims, new WriteLock());
        _activePolicy = Policy.Create(Guid.NewGuid(), ProductType.Auto, Jan1, Dec31, 100m).Value.Activate([]).Value;
        _policies.Save(_activePolicy);
    }

    private Claim CreateClaim() => _service.Create(_activePolicy.Id, Jan1, 50m).Value;

    [Fact]
    public void Create_WhenValid_ThenStoresNewClaim()
    {
        var result = _service.Create(_activePolicy.Id, Dec31, 75.5m);

        Assert.True(result.IsSuccess);
        Assert.Equal(ClaimStatus.New, result.Value.Status);
        Assert.Same(result.Value, _claims.FindById(result.Value.Id));
    }

    [Fact]
    public void Create_WhenPolicyMissing_ThenReferencedPolicyNotFound()
    {
        var result = _service.Create(Guid.NewGuid(), Jan1, 50m);

        Assert.Same(ApplicationErrors.ReferencedPolicyNotFound, result.Error);
    }

    [Fact]
    public void Create_WhenPolicyIsDraft_ThenPolicyNotActiveAndNotSaved()
    {
        var draft = Policy.Create(Guid.NewGuid(), ProductType.Auto, Jan1, Dec31, 100m).Value;
        _policies.Save(draft);
        var claims = new Mock<IClaimRepository>();
        var service = new ClaimService(_policies, claims.Object, new WriteLock());

        var result = service.Create(draft.Id, Jan1, 50m);

        Assert.Same(ClaimErrors.PolicyNotActive, result.Error);
        claims.Verify(r => r.Save(It.IsAny<Claim>()), Times.Never);
    }

    [Fact]
    public void Create_WhenIncidentOutsidePeriod_ThenIncidentOutsidePolicyPeriod()
    {
        var result = _service.Create(_activePolicy.Id, Dec31.AddDays(1), 50m);

        Assert.Same(ClaimErrors.IncidentOutsidePolicyPeriod, result.Error);
    }

    [Fact]
    public void Decide_WhenRejectingNewClaim_ThenStoresRejectedClaim()
    {
        var claim = CreateClaim();

        var result = _service.Decide(claim.Id, ClaimDecision.Reject, "Not covered");

        Assert.Equal(ClaimStatus.Rejected, result.Value.Status);
        var stored = _claims.FindById(claim.Id)!;
        Assert.Equal(ClaimStatus.Rejected, stored.Status);
        Assert.Equal("Not covered", stored.DecisionReason);
    }

    [Fact]
    public void Decide_WhenMissing_ThenClaimNotFound()
    {
        var result = _service.Decide(Guid.NewGuid(), ClaimDecision.Approve, null);

        Assert.Same(ApplicationErrors.ClaimNotFound, result.Error);
    }

    [Fact]
    public void Decide_WhenAlreadyDecided_ThenAlreadyDecidedAndStoredStatusUnchanged()
    {
        var claim = CreateClaim();
        _service.Decide(claim.Id, ClaimDecision.Approve, null);

        var result = _service.Decide(claim.Id, ClaimDecision.Reject, "Changed my mind");

        Assert.Same(ClaimErrors.AlreadyDecided, result.Error);
        Assert.Equal(ClaimStatus.Approved, _claims.FindById(claim.Id)!.Status);
    }

    [Fact]
    public void Decide_WhenRejectingWithoutReason_ThenDecisionReasonRequiredAndStaysNew()
    {
        var claim = CreateClaim();

        var result = _service.Decide(claim.Id, ClaimDecision.Reject, " ");

        Assert.Same(ClaimErrors.DecisionReasonRequired, result.Error);
        Assert.Equal(ClaimStatus.New, _claims.FindById(claim.Id)!.Status);
    }

    [Fact]
    public async Task Decide_WhenSameClaimDecidedConcurrently_ThenExactlyOneSucceeds()
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var claim = CreateClaim();

            var results = await Concurrently.RunAsync(8, i =>
                _service.Decide(claim.Id, i % 2 == 0 ? ClaimDecision.Approve : ClaimDecision.Reject, "Reason"));

            var success = Assert.Single(results, r => r.IsSuccess);
            Assert.Equal(success.Value.Status, _claims.FindById(claim.Id)!.Status);
        }
    }

    [Fact]
    public void Get_WhenExists_ThenReturnsClaim()
    {
        var claim = CreateClaim();

        Assert.Same(claim, _service.Get(claim.Id).Value);
    }

    [Fact]
    public void Get_WhenMissing_ThenClaimNotFound()
    {
        Assert.Same(ApplicationErrors.ClaimNotFound, _service.Get(Guid.NewGuid()).Error);
    }
}
