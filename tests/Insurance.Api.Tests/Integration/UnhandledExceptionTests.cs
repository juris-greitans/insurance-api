using System.Net;
using Insurance.Api.Application.Abstractions;
using Insurance.Api.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Insurance.Api.Tests.Integration;

public class UnhandledExceptionTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SecretMessage = "secret internal failure";

    [Fact]
    public async Task Get_WhenRepositoryThrows_ThenInternalServerErrorProblemWithoutExceptionDetails()
    {
        var customers = new Mock<ICustomerRepository>();
        customers.Setup(r => r.FindById(It.IsAny<Guid>())).Throws(new InvalidOperationException(SecretMessage));
        using var failingFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.Replace(ServiceDescriptor.Singleton(customers.Object))));
        using var client = failingFactory.CreateClient();
        var api = new ApiTestClient(client);

        var response = await api.GetAsync($"/customers/{Guid.NewGuid()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(SecretMessage, body, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(InvalidOperationException), body, StringComparison.Ordinal);
    }
}
