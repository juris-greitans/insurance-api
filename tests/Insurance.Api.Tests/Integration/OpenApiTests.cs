using System.Net;
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

    [Fact]
    public async Task Get_WhenRouteDoesNotExist_ThenReturnsNotFoundProblemDetails()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/does-not-exist", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
