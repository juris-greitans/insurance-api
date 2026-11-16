using System.Net;
using Insurance.Api.Controllers.Contracts;
using Insurance.Api.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Insurance.Api.Tests.Integration.Controllers;

public class CustomersControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Fact]
    public async Task Create_WhenValid_ThenReturnsCreatedWithLocation()
    {
        var response = await _api.PostAsync("/customers", new { fullName = "  Jane Doe  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var customer = await ApiTestClient.ReadSuccessAsync<CustomerResponse>(response);
        Assert.Equal("Jane Doe", customer.FullName);
        Assert.Equal($"/customers/{customer.Id}", response.Headers.Location?.AbsolutePath, ignoreCase: true);
    }

    [Fact]
    public async Task Create_WhenCreated_ThenLocationReturnsSameCustomer()
    {
        var created = await _api.PostAsync("/customers", new { fullName = "Jane Doe" });
        var customer = await ApiTestClient.ReadSuccessAsync<CustomerResponse>(created);

        var fetched = await ApiTestClient.ReadSuccessAsync<CustomerResponse>(
            await _api.GetAsync(created.Headers.Location!.AbsolutePath));

        Assert.Equal(customer, fetched);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"fullName\":null}")]
    [InlineData("{\"fullName\":\"\"}")]
    [InlineData("{\"fullName\":\"   \"}")]
    public async Task Create_WhenFullNameMissingOrBlank_ThenBadRequestWithCode(string json)
    {
        var response = await _api.PostRawAsync("/customers", json);

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest, "Customer.FullNameRequired");
    }

    [Fact]
    public async Task Create_WhenFullNameAtMaxLength_ThenCreated()
    {
        var response = await _api.PostAsync("/customers", new { fullName = new string('a', 250) });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenFullNameTooLong_ThenBadRequestWithCode()
    {
        var response = await _api.PostAsync("/customers", new { fullName = new string('a', 251) });

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest, "Customer.FullNameTooLong");
    }

    [Fact]
    public async Task Create_WhenBodyEmpty_ThenBadRequest()
    {
        var response = await _api.PostRawAsync("/customers", "");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WhenJsonMalformed_ThenBadRequest()
    {
        var response = await _api.PostRawAsync("/customers", "{ \"fullName\": ");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_WhenMissing_ThenNotFoundWithCode()
    {
        var response = await _api.GetAsync($"/customers/{Guid.NewGuid()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, "Customer.NotFound");
    }

    [Fact]
    public async Task Get_WhenIdNotAGuid_ThenValidationProblem()
    {
        var response = await _api.GetAsync("/customers/not-a-guid");

        await ProblemAssert.IsValidationProblemAsync(response, "id");
    }
}
