using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Insurance.Api.Controllers.Contracts;

namespace Insurance.Api.Tests.TestSupport;

/// <summary>Thin wrapper over <see cref="HttpClient"/> for integration tests.</summary>
internal sealed class ApiTestClient(HttpClient client)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public Task<HttpResponseMessage> GetAsync(string url) => client.GetAsync(Relative(url));

    public Task<HttpResponseMessage> PostAsync(string url, object? body = null) =>
        client.PostAsJsonAsync(Relative(url), body, Json);

    public Task<HttpResponseMessage> PostRawAsync(string url, string json) =>
        client.PostAsync(Relative(url), new StringContent(json, Encoding.UTF8, "application/json"));

    public async Task<CustomerResponse> CreateCustomerAsync(string fullName = "Jane Doe") =>
        await ReadSuccessAsync<CustomerResponse>(await PostAsync("/customers", new { fullName }));

    public async Task<PolicyResponse> CreatePolicyAsync(
        Guid customerId,
        string productType = "Auto",
        string startDate = "2026-01-01",
        string endDate = "2026-12-31",
        decimal premium = 100m) =>
        await ReadSuccessAsync<PolicyResponse>(await PostAsync(
            "/policies",
            new { customerId, productType, startDate, endDate, premium }));

    public async Task<PolicyResponse> CreateActivePolicyAsync(
        string startDate = "2026-01-01",
        string endDate = "2026-12-31")
    {
        var customer = await CreateCustomerAsync();
        var policy = await CreatePolicyAsync(customer.Id, startDate: startDate, endDate: endDate);
        return await ReadSuccessAsync<PolicyResponse>(await PostAsync($"/policies/{policy.Id}/activate"));
    }

    public async Task<ClaimResponse> CreateClaimAsync(
        Guid policyId,
        string incidentDate = "2026-06-15",
        decimal amountRequested = 50m) =>
        await ReadSuccessAsync<ClaimResponse>(await PostAsync(
            "/claims",
            new { policyId, incidentDate, amountRequested }));

    public static async Task<T> ReadSuccessAsync<T>(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        Assert.True(
            response.IsSuccessStatusCode,
            $"Expected success but got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private static Uri Relative(string url) => new(url, UriKind.Relative);
}
