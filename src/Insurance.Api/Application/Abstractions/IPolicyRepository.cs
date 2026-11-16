using Insurance.Api.Domain.Policies;

namespace Insurance.Api.Application.Abstractions;

public interface IPolicyRepository
{
    Policy? FindById(Guid id);

    /// <summary>Returns a snapshot of all policies of the customer, in any status.</summary>
    IReadOnlyCollection<Policy> GetByCustomer(Guid customerId);

    /// <summary>Inserts or replaces the policy with the same id.</summary>
    void Save(Policy policy);
}
