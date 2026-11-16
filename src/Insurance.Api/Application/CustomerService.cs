using Insurance.Api.Application.Abstractions;
using Insurance.Api.Domain.Common;
using Insurance.Api.Domain.Customers;

namespace Insurance.Api.Application;

public sealed class CustomerService(ICustomerRepository customers, WriteLock writeLock)
{
    public Result<Customer> Create(string? fullName) => writeLock.Execute(() => CreateLocked(fullName));

    public Result<Customer> Get(Guid id)
    {
        var customer = customers.FindById(id);
        return customer is null ? ApplicationErrors.CustomerNotFound : customer;
    }

    private Result<Customer> CreateLocked(string? fullName)
    {
        var result = Customer.Create(fullName);
        if (result.IsSuccess)
        {
            customers.Save(result.Value);
        }

        return result;
    }
}
