using System.Net;
using System.Text.Json;

namespace Insurance.Api.Tests.TestSupport;

internal static class ProblemAssert
{
    /// <summary>
    /// Asserts a ProblemDetails response: content type, <c>status</c>, <c>type</c>, <c>title</c> and <c>traceId</c>.
    /// When <paramref name="expectedCode"/> is given, also the domain error <c>code</c> and a <c>detail</c> message.
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
        AssertNonEmptyString(problem, "type");
        AssertNonEmptyString(problem, "title");
        AssertNonEmptyString(problem, "traceId");

        if (expectedCode is not null)
        {
            Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
            AssertNonEmptyString(problem, "detail");
        }

        return problem;
    }

    /// <summary>
    /// Asserts a 400 ValidationProblemDetails with an error entry for exactly <paramref name="field"/>.
    /// Keys are compared ignoring case and the JSON path prefix (<c>$.</c>) that serializer errors use.
    /// </summary>
    public static async Task IsValidationProblemAsync(HttpResponseMessage response, string field)
    {
        var problem = await IsProblemAsync(response, HttpStatusCode.BadRequest);
        var keys = problem.GetProperty("errors").EnumerateObject().Select(e => e.Name).ToList();
        Assert.True(
            keys.Exists(key => string.Equals(StripJsonPath(key), field, StringComparison.OrdinalIgnoreCase)),
            $"Expected a validation error for '{field}', got: {string.Join(", ", keys)}");
    }

    private static string StripJsonPath(string key) => key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;

    private static void AssertNonEmptyString(JsonElement problem, string property)
    {
        Assert.True(problem.TryGetProperty(property, out var value), $"ProblemDetails must have '{property}'.");
        Assert.False(string.IsNullOrWhiteSpace(value.GetString()), $"ProblemDetails '{property}' must not be empty.");
    }
}
