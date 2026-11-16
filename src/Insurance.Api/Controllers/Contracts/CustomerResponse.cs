using Insurance.Api.Domain.Customers;

namespace Insurance.Api.Controllers.Contracts;

public sealed record CustomerResponse(Guid Id, string FullName)
{
    public static CustomerResponse From(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        return new CustomerResponse(customer.Id, customer.FullName);
    }
}
