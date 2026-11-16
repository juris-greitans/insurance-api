using Insurance.Api.Application;
using Insurance.Api.Application.Abstractions;
using Insurance.Api.Domain.Customers;
using Insurance.Api.Infrastructure;
using Moq;

namespace Insurance.Api.Tests.Unit.Application;

public class CustomerServiceTests
{
    private readonly InMemoryCustomerRepository _customers = new();
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _service = new CustomerService(_customers, new WriteLock());
    }

    [Fact]
    public void Create_WhenValid_ThenStoresAndReturnsCustomer()
    {
        var result = _service.Create("Jane Doe");

        Assert.True(result.IsSuccess);
        Assert.Same(result.Value, _customers.FindById(result.Value.Id));
    }

    [Fact]
    public void Create_WhenInvalid_ThenReturnsDomainErrorAndDoesNotSave()
    {
        var repository = new Mock<ICustomerRepository>();
        var service = new CustomerService(repository.Object, new WriteLock());

        var result = service.Create(" ");

        Assert.Same(CustomerErrors.FullNameRequired, result.Error);
        repository.Verify(r => r.Save(It.IsAny<Customer>()), Times.Never);
    }

    [Fact]
    public void Get_WhenExists_ThenReturnsCustomer()
    {
        var created = _service.Create("Jane Doe").Value;

        var result = _service.Get(created.Id);

        Assert.Same(created, result.Value);
    }

    [Fact]
    public void Get_WhenMissing_ThenCustomerNotFound()
    {
        var result = _service.Get(Guid.NewGuid());

        Assert.Same(ApplicationErrors.CustomerNotFound, result.Error);
    }
}
