using Insurance.Api.Domain.Policies;
using Insurance.Api.Infrastructure;

namespace Insurance.Api.Tests.Unit.Infrastructure;

public class InMemoryPolicyRepositoryTests
{
    private static readonly DateOnly Jan1 = new(2026, 1, 1);
    private static readonly DateOnly Dec31 = new(2026, 12, 31);

    private readonly InMemoryPolicyRepository _repository = new();

    private static Policy NewPolicy(Guid customerId, ProductType productType = ProductType.Auto) =>
        Policy.Create(customerId, productType, Jan1, Dec31, 100m).Value;

    [Fact]
    public void FindById_WhenIdUnknown_ThenReturnsNull()
    {
        Assert.Null(_repository.FindById(Guid.NewGuid()));
    }

    [Fact]
    public void Save_WhenPolicyWithSameIdExists_ThenReplacesIt()
    {
        var draft = NewPolicy(Guid.NewGuid());
        var active = draft.Activate([]).Value;
        _repository.Save(draft);

        _repository.Save(active);

        Assert.Same(active, _repository.FindById(draft.Id));
        Assert.Single(_repository.GetByCustomer(draft.CustomerId));
    }

    [Fact]
    public async Task Save_WhenCalledConcurrently_ThenAllPoliciesStored()
    {
        var customerId = Guid.NewGuid();

        await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(() => _repository.Save(NewPolicy(customerId)))));

        Assert.Equal(100, _repository.GetByCustomer(customerId).Count);
    }

    [Fact]
    public void GetByCustomer_WhenCustomerHasPolicies_ThenReturnsOnlyTheirs()
    {
        var customerId = Guid.NewGuid();
        var auto = NewPolicy(customerId, ProductType.Auto);
        var travel = NewPolicy(customerId, ProductType.Travel);
        _repository.Save(auto);
        _repository.Save(travel);
        _repository.Save(NewPolicy(Guid.NewGuid()));

        var policies = _repository.GetByCustomer(customerId);

        Assert.Equal(2, policies.Count);
        Assert.Contains(auto, policies);
        Assert.Contains(travel, policies);
    }

    [Fact]
    public void GetByCustomer_WhenCustomerHasNoPolicies_ThenReturnsEmpty()
    {
        _repository.Save(NewPolicy(Guid.NewGuid()));

        Assert.Empty(_repository.GetByCustomer(Guid.NewGuid()));
    }

    [Fact]
    public void GetByCustomer_WhenPolicySavedAfterwards_ThenReturnedCollectionUnchanged()
    {
        var customerId = Guid.NewGuid();
        _repository.Save(NewPolicy(customerId));
        var snapshot = _repository.GetByCustomer(customerId);

        _repository.Save(NewPolicy(customerId));

        Assert.Single(snapshot);
    }
}
