using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Insurance.Api.Tests.Integration;

public class OpenApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/swagger/index.html")]
    public async Task Get_WhenDocumentationEndpointRequested_ThenReturnsOk(string url)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri(url, UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("ProductType", new[] { "Auto", "Property", "Travel" })]
    [InlineData("ClaimDecision", new[] { "Approve", "Reject" })]
    [InlineData("PolicyStatus", new[] { "Draft", "Active" })]
    [InlineData("ClaimStatus", new[] { "New", "Approved", "Rejected" })]
    public async Task Get_WhenOpenApiDocumentRequested_ThenEnumsAreDocumentedAsNames(string schema, string[] names)
    {
        using var client = factory.CreateClient();

        var json = await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        using var document = JsonDocument.Parse(json);
        var values = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty(schema)
            .GetProperty("enum").EnumerateArray()
            .Where(v => v.ValueKind == JsonValueKind.String)
            .Select(v => v.GetString());
        Assert.Equal(names, values);
    }

    [Fact]
    public async Task Get_WhenRouteDoesNotExist_ThenReturnsNotFoundProblemDetails()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/does-not-exist", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
