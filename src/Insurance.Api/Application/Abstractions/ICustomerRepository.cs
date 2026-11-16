using Insurance.Api.Domain.Customers;

namespace Insurance.Api.Application.Abstractions;

public interface ICustomerRepository
{
    Customer? FindById(Guid id);

    /// <summary>Inserts or replaces the customer with the same id.</summary>
    void Save(Customer customer);
}
