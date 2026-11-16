using Insurance.Api.Domain.Customers;
using Insurance.Api.Infrastructure;

namespace Insurance.Api.Tests.Unit.Infrastructure;

public class InMemoryCustomerRepositoryTests
{
    private readonly InMemoryCustomerRepository _repository = new();

    [Fact]
    public void FindById_WhenIdUnknown_ThenReturnsNull()
    {
        Assert.Null(_repository.FindById(Guid.NewGuid()));
    }

    [Fact]
    public void Save_WhenNew_ThenFindByIdReturnsIt()
    {
        var customer = Customer.Create("Jane Doe").Value;

        _repository.Save(customer);

        Assert.Same(customer, _repository.FindById(customer.Id));
    }

    [Fact]
    public void Save_WhenEntityNull_ThenThrows()
    {
        Assert.Throws<ArgumentNullException>(() => _repository.Save(null!));
    }
}
