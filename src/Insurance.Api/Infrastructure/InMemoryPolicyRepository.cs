using Insurance.Api.Application.Abstractions;
using Insurance.Api.Domain.Policies;

namespace Insurance.Api.Infrastructure;

public sealed class InMemoryPolicyRepository() : InMemoryRepository<Policy>(p => p.Id), IPolicyRepository
{
    public IReadOnlyCollection<Policy> GetByCustomer(Guid customerId) =>
        Items.Where(p => p.CustomerId == customerId).ToList();
}
