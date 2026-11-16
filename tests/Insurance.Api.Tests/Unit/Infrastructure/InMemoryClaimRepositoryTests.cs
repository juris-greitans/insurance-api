using Insurance.Api.Domain.Claims;
using Insurance.Api.Domain.Policies;
using Insurance.Api.Infrastructure;

namespace Insurance.Api.Tests.Unit.Infrastructure;

public class InMemoryClaimRepositoryTests
{
    private static readonly DateOnly Jan1 = new(2026, 1, 1);

    private readonly InMemoryClaimRepository _repository = new();

    private static Claim NewClaim()
    {
        var policy = Policy.Create(Guid.NewGuid(), ProductType.Auto, Jan1, Jan1.AddYears(1), 100m).Value
            .Activate([]).Value;
        return Claim.Create(policy, Jan1, 10m).Value;
    }

    [Fact]
    public void FindById_WhenIdUnknown_ThenReturnsNull()
    {
        Assert.Null(_repository.FindById(Guid.NewGuid()));
    }

    [Fact]
    public void Save_WhenNew_ThenFindByIdReturnsIt()
    {
        var claim = NewClaim();

        _repository.Save(claim);

        Assert.Same(claim, _repository.FindById(claim.Id));
    }

    [Fact]
    public void Save_WhenClaimWithSameIdExists_ThenReplacesIt()
    {
        var claim = NewClaim();
        var approved = claim.Decide(ClaimDecision.Approve, null).Value;
        _repository.Save(claim);

        _repository.Save(approved);

        Assert.Same(approved, _repository.FindById(claim.Id));
    }
}
