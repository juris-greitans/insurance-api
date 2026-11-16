using Insurance.Api.Application.Abstractions;
using Insurance.Api.Domain.Customers;

namespace Insurance.Api.Infrastructure;

public sealed class InMemoryCustomerRepository() : InMemoryRepository<Customer>(c => c.Id), ICustomerRepository;
