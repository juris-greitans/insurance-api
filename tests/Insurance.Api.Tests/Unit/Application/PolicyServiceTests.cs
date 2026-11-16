using Insurance.Api.Application;
using Insurance.Api.Application.Abstractions;
using Insurance.Api.Domain.Customers;
using Insurance.Api.Domain.Policies;
using Insurance.Api.Infrastructure;
using Insurance.Api.Tests.TestSupport;
using Moq;

namespace Insurance.Api.Tests.Unit.Application;

public class PolicyServiceTests
{
    private static readonly DateOnly Jan1 = new(2026, 1, 1);
    private static readonly DateOnly Dec31 = new(2026, 12, 31);

    private readonly InMemoryCustomerRepository _customers = new();
    private readonly InMemoryPolicyRepository _policies = new();
    private readonly PolicyService _service;
    private readonly Customer _customer;

    public PolicyServiceTests()
    {
        _service = new PolicyService(_customers, _policies, new WriteLock());
        _customer = Customer.Create("Jane Doe").Value;
        _customers.Save(_customer);
    }

    /// <summary>Setup step: activates a policy and fails the test if that doesn't succeed.</summary>
    private void ActivateExisting(Guid policyId) => Assert.True(_service.Activate(policyId).IsSuccess);

    private Policy CreateDraft(DateOnly start, DateOnly end, ProductType productType = ProductType.Auto) =>
        _service.Create(_customer.Id, productType, start, end, 100m).Value;

    [Fact]
    public void Create_WhenValid_ThenStoresDraftPolicy()
    {
        var result = _service.Create(_customer.Id, ProductType.Property, Jan1, Dec31, 250m);

        Assert.True(result.IsSuccess);
        Assert.Equal(PolicyStatus.Draft, result.Value.Status);
        Assert.Same(result.Value, _policies.FindById(result.Value.Id));
    }

    [Fact]
    public void Create_WhenCustomerMissing_ThenReferencedCustomerNotFoundAndNotSaved()
    {
        var policies = new Mock<IPolicyRepository>();
        var service = new PolicyService(_customers, policies.Object, new WriteLock());

        var result = service.Create(Guid.NewGuid(), ProductType.Auto, Jan1, Dec31, 100m);

        Assert.Same(ApplicationErrors.ReferencedCustomerNotFound, result.Error);
        policies.Verify(r => r.Save(It.IsAny<Policy>()), Times.Never);
    }

    [Fact]
    public void Create_WhenPeriodInvalid_ThenDomainErrorAndNotSaved()
    {
        var policies = new Mock<IPolicyRepository>();
        var service = new PolicyService(_customers, policies.Object, new WriteLock());

        var result = service.Create(_customer.Id, ProductType.Auto, Dec31, Jan1, 100m);

        Assert.Same(PolicyErrors.InvalidPeriod, result.Error);
        policies.Verify(r => r.Save(It.IsAny<Policy>()), Times.Never);
    }

    [Fact]
    public void Activate_WhenDraft_ThenStoresActivePolicy()
    {
        var draft = CreateDraft(Jan1, Dec31);

        var result = _service.Activate(draft.Id);

        Assert.Equal(PolicyStatus.Active, result.Value.Status);
        Assert.Equal(PolicyStatus.Active, _policies.FindById(draft.Id)!.Status);
    }

    [Fact]
    public void Activate_WhenMissing_ThenPolicyNotFound()
    {
        var result = _service.Activate(Guid.NewGuid());

        Assert.Same(ApplicationErrors.PolicyNotFound, result.Error);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ThenNotDraft()
    {
        var draft = CreateDraft(Jan1, Dec31);
        ActivateExisting(draft.Id);

        var result = _service.Activate(draft.Id);

        Assert.Same(PolicyErrors.NotDraft, result.Error);
    }

    [Fact]
    public void Activate_WhenStoredActivePolicyOverlaps_ThenOverlappingActivePolicyAndStaysDraft()
    {
        var first = CreateDraft(Jan1, new DateOnly(2026, 6, 30));
        var second = CreateDraft(new DateOnly(2026, 6, 30), Dec31);
        ActivateExisting(first.Id);

        var result = _service.Activate(second.Id);

        Assert.Same(PolicyErrors.OverlappingActivePolicy, result.Error);
        Assert.Equal(PolicyStatus.Draft, _policies.FindById(second.Id)!.Status);
    }

    [Fact]
    public void Activate_WhenOverlappingPolicyHasOtherProductType_ThenSucceeds()
    {
        var auto = CreateDraft(Jan1, Dec31, ProductType.Auto);
        var travel = CreateDraft(Jan1, Dec31, ProductType.Travel);
        ActivateExisting(auto.Id);

        var result = _service.Activate(travel.Id);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Activate_WhenOverlappingDraftsActivatedConcurrently_ThenExactlyOneSucceeds()
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var customer = Customer.Create("Concurrent").Value;
            _customers.Save(customer);
            var drafts = Enumerable.Range(0, 8)
                .Select(_ => _service.Create(customer.Id, ProductType.Auto, Jan1, Dec31, 100m).Value)
                .ToList();

            var results = await Concurrently.RunAsync(drafts.Count, i => _service.Activate(drafts[i].Id));

            Assert.Single(results, r => r.IsSuccess);
            Assert.Single(_policies.GetByCustomer(customer.Id), p => p.Status == PolicyStatus.Active);
        }
    }

    [Fact]
    public void Get_WhenExists_ThenReturnsPolicy()
    {
        var draft = CreateDraft(Jan1, Dec31);

        Assert.Same(draft, _service.Get(draft.Id).Value);
    }

    [Fact]
    public void Get_WhenMissing_ThenPolicyNotFound()
    {
        Assert.Same(ApplicationErrors.PolicyNotFound, _service.Get(Guid.NewGuid()).Error);
    }
}
