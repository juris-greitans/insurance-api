using Insurance.Api.Application.Abstractions;
using Insurance.Api.Domain.Common;
using Insurance.Api.Domain.Policies;

namespace Insurance.Api.Application;

public sealed class PolicyService(ICustomerRepository customers, IPolicyRepository policies, WriteLock writeLock)
{
    public Result<Policy> Create(
        Guid customerId,
        ProductType productType,
        DateOnly startDate,
        DateOnly endDate,
        decimal premium) =>
        writeLock.Execute(() => CreateLocked(customerId, productType, startDate, endDate, premium));

    public Result<Policy> Activate(Guid id) => writeLock.Execute(() => ActivateLocked(id));

    public Result<Policy> Get(Guid id)
    {
        var policy = policies.FindById(id);
        return policy is null ? ApplicationErrors.PolicyNotFound : policy;
    }

    private Result<Policy> CreateLocked(
        Guid customerId,
        ProductType productType,
        DateOnly startDate,
        DateOnly endDate,
        decimal premium)
    {
        var result = Policy.Create(customerId, productType, startDate, endDate, premium);
        if (result.IsFailure)
        {
            return result;
        }

        if (customers.FindById(customerId) is null)
        {
            return ApplicationErrors.ReferencedCustomerNotFound;
        }

        policies.Save(result.Value);
        return result;
    }

    private Result<Policy> ActivateLocked(Guid id)
    {
        var policy = policies.FindById(id);
        if (policy is null)
        {
            return ApplicationErrors.PolicyNotFound;
        }

        var result = policy.Activate(policies.GetByCustomer(policy.CustomerId));
        if (result.IsSuccess)
        {
            policies.Save(result.Value);
        }

        return result;
    }
}
