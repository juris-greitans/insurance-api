using System.Net;
using System.Text.Json;

namespace Insurance.Api.Tests.TestSupport;

internal static class ProblemAssert
{
    /// <summary>
    /// Asserts a ProblemDetails response with the given status and, when given, the domain error code.
    /// </summary>
    public static async Task<JsonElement> IsProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string? expectedCode = null)
    {
        ArgumentNullException.ThrowIfNull(response);
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problem = document.RootElement.Clone();
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("title", out _), "ProblemDetails must have a title.");

        if (expectedCode is not null)
        {
            Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
        }

        return problem;
    }

    /// <summary>Asserts a 400 ValidationProblemDetails with an error entry for <paramref name="field"/>.</summary>
    public static async Task IsValidationProblemAsync(HttpResponseMessage response, string field)
    {
        var problem = await IsProblemAsync(response, HttpStatusCode.BadRequest);
        var errors = problem.GetProperty("errors");
        Assert.Contains(
            errors.EnumerateObject(),
            e => e.Name.Contains(field, StringComparison.OrdinalIgnoreCase));
    }
}
