using Insurance.Api.Domain.Policies;

namespace Insurance.Api.Controllers.Contracts;

public sealed record PolicyResponse(
    Guid Id,
    Guid CustomerId,
    ProductType ProductType,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Premium,
    PolicyStatus Status)
{
    public static PolicyResponse From(Policy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return new PolicyResponse(
            policy.Id,
            policy.CustomerId,
            policy.ProductType,
            policy.StartDate,
            policy.EndDate,
            policy.Premium,
            policy.Status);
    }
}
