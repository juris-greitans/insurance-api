using System.Net;
using Insurance.Api.Controllers.Contracts;
using Insurance.Api.Domain.Policies;
using Insurance.Api.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Insurance.Api.Tests.Integration.Controllers;

public class PoliciesControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private async Task<object> ValidRequestAsync() => new
    {
        customerId = (await _api.CreateCustomerAsync()).Id,
        productType = "Auto",
        startDate = "2026-01-01",
        endDate = "2026-12-31",
        premium = 100.50m,
    };

    private Task<HttpResponseMessage> ActivateAsync(Guid policyId) => _api.PostAsync($"/policies/{policyId}/activate");

    /// <summary>Setup step: activates a policy and fails the test if that doesn't succeed.</summary>
    private async Task ActivateExistingAsync(Guid policyId) =>
        await ApiTestClient.ReadSuccessAsync<PolicyResponse>(await ActivateAsync(policyId));

    [Fact]
    public async Task Create_WhenValid_ThenReturnsCreatedDraftWithLocation()
    {
        var customer = await _api.CreateCustomerAsync();

        var response = await _api.PostAsync("/policies", new
        {
            customerId = customer.Id,
            productType = "Travel",
            startDate = "2026-01-01",
            endDate = "2026-01-02",
            premium = 0.01m,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var policy = await ApiTestClient.ReadSuccessAsync<PolicyResponse>(response);
        Assert.Equal(
            new PolicyResponse(
                policy.Id,
                customer.Id,
                ProductType.Travel,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 2),
                0.01m,
                PolicyStatus.Draft),
            policy);
        Assert.Equal($"/policies/{policy.Id}", response.Headers.Location?.AbsolutePath, ignoreCase: true);
    }

    [Fact]
    public async Task Create_WhenValid_ThenSerializesEnumsAsStringsAndDatesAsIso()
    {
        var response = await _api.PostAsync("/policies", await ValidRequestAsync());

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"productType\":\"Auto\"", json, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"Draft\"", json, StringComparison.Ordinal);
        Assert.Contains("\"startDate\":\"2026-01-01\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_WhenCustomerMissing_ThenBadRequestWithCode()
    {
        var response = await _api.PostAsync("/policies", new
        {
            customerId = Guid.NewGuid(),
            productType = "Auto",
            startDate = "2026-01-01",
            endDate = "2026-12-31",
            premium = 100m,
        });

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest, "Policy.CustomerNotFound");
    }

    [Theory]
    [InlineData("2026-01-01", "2026-01-01")]
    [InlineData("2026-12-31", "2026-01-01")]
    public async Task Create_WhenEndDateNotAfterStartDate_ThenBadRequestWithCode(string startDate, string endDate)
    {
        var customer = await _api.CreateCustomerAsync();

        var response = await _api.PostAsync("/policies", new
        {
            customerId = customer.Id,
            productType = "Auto",
            startDate,
            endDate,
            premium = 100m,
        });

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest, "Policy.InvalidPeriod");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("10.001")]
    public async Task Create_WhenPremiumInvalid_ThenBadRequestWithCode(string premium)
    {
        var customer = await _api.CreateCustomerAsync();

        var response = await _api.PostRawAsync(
            "/policies",
            $$"""{"customerId":"{{customer.Id}}","productType":"Auto","startDate":"2026-01-01","endDate":"2026-12-31","premium":{{premium}}}""");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest, "Policy.InvalidPremium");
    }

    [Theory]
    [InlineData("customerId")]
    [InlineData("productType")]
    [InlineData("startDate")]
    [InlineData("endDate")]
    [InlineData("premium")]
    public async Task Create_WhenFieldMissing_ThenValidationProblem(string missingField)
    {
        var customer = await _api.CreateCustomerAsync();
        var fields = new Dictionary<string, object>
        {
            ["customerId"] = customer.Id,
            ["productType"] = "Auto",
            ["startDate"] = "2026-01-01",
            ["endDate"] = "2026-12-31",
            ["premium"] = 100m,
        };
        fields.Remove(missingField);

        var response = await _api.PostAsync("/policies", fields);

        await ProblemAssert.IsValidationProblemAsync(response, missingField);
    }

    [Theory]
    [InlineData("productType", "\"Boat\"")]
    [InlineData("productType", "0")]
    [InlineData("productType", "\"auto\"")]
    [InlineData("productType", "\" Auto\"")]
    [InlineData("productType", "\"Auto, Property\"")]
    [InlineData("startDate", "\"01/01/2026\"")]
    [InlineData("endDate", "\"not-a-date\"")]
    [InlineData("premium", "\"lots\"")]
    [InlineData("customerId", "\"not-a-guid\"")]
    public async Task Create_WhenFieldMalformed_ThenValidationProblem(string field, string value)
    {
        var customer = await _api.CreateCustomerAsync();
        var fields = new Dictionary<string, string>
        {
            ["customerId"] = $"\"{customer.Id}\"",
            ["productType"] = "\"Auto\"",
            ["startDate"] = "\"2026-01-01\"",
            ["endDate"] = "\"2026-12-31\"",
            ["premium"] = "100",
        };
        fields[field] = value;
        var json = "{" + string.Join(",", fields.Select(f => $"\"{f.Key}\":{f.Value}")) + "}";

        var response = await _api.PostRawAsync("/policies", json);

        await ProblemAssert.IsValidationProblemAsync(response, field);
    }

    [Fact]
    public async Task Activate_WhenDraft_ThenReturnsActivePolicy()
    {
        var customer = await _api.CreateCustomerAsync();
        var draft = await _api.CreatePolicyAsync(customer.Id);

        var response = await ActivateAsync(draft.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var active = await ApiTestClient.ReadSuccessAsync<PolicyResponse>(response);
        Assert.Equal(draft with { Status = PolicyStatus.Active }, active);
    }

    [Fact]
    public async Task Activate_WhenAlreadyActive_ThenConflictWithCode()
    {
        var active = await _api.CreateActivePolicyAsync();

        var response = await ActivateAsync(active.Id);

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, "Policy.NotDraft");
    }

    [Fact]
    public async Task Activate_WhenMissing_ThenNotFoundWithCode()
    {
        var response = await ActivateAsync(Guid.NewGuid());

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, "Policy.NotFound");
    }

    [Theory]
    [InlineData("2026-01-01", "2026-12-31", "2026-03-01", "2026-04-01")] // inside
    [InlineData("2026-01-01", "2026-06-30", "2026-06-30", "2026-12-31")] // touching on one day
    [InlineData("2026-06-01", "2026-12-31", "2026-01-01", "2026-06-01")] // touching on one day, other side
    public async Task Activate_WhenOverlapsActivePolicyOfSameType_ThenConflictWithCode(
        string activeStart, string activeEnd, string draftStart, string draftEnd)
    {
        var customer = await _api.CreateCustomerAsync();
        var existing = await _api.CreatePolicyAsync(customer.Id, "Property", activeStart, activeEnd);
        await ActivateExistingAsync(existing.Id);
        var draft = await _api.CreatePolicyAsync(customer.Id, "Property", draftStart, draftEnd);

        var response = await ActivateAsync(draft.Id);

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, "Policy.OverlappingActivePolicy");
        var stored = await ApiTestClient.ReadSuccessAsync<PolicyResponse>(await _api.GetAsync($"/policies/{draft.Id}"));
        Assert.Equal(PolicyStatus.Draft, stored.Status);
    }

    [Fact]
    public async Task Activate_WhenAdjacentToActivePolicyOfSameType_ThenOk()
    {
        var customer = await _api.CreateCustomerAsync();
        var existing = await _api.CreatePolicyAsync(customer.Id, "Auto", "2026-01-01", "2026-06-29");
        await ActivateExistingAsync(existing.Id);
        var draft = await _api.CreatePolicyAsync(customer.Id, "Auto", "2026-06-30", "2026-12-31");

        var response = await ActivateAsync(draft.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Activate_WhenOverlappingActivePolicyHasOtherTypeOrCustomer_ThenOk()
    {
        var customer = await _api.CreateCustomerAsync();
        var otherCustomer = await _api.CreateCustomerAsync();
        await ActivateExistingAsync((await _api.CreatePolicyAsync(customer.Id, "Travel")).Id);
        await ActivateExistingAsync((await _api.CreatePolicyAsync(otherCustomer.Id, "Auto")).Id);
        var draft = await _api.CreatePolicyAsync(customer.Id, "Auto");

        var response = await ActivateAsync(draft.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenExists_ThenReturnsPolicy()
    {
        var customer = await _api.CreateCustomerAsync();
        var draft = await _api.CreatePolicyAsync(customer.Id);

        var fetched = await ApiTestClient.ReadSuccessAsync<PolicyResponse>(await _api.GetAsync($"/policies/{draft.Id}"));

        Assert.Equal(draft, fetched);
    }

    [Fact]
    public async Task Get_WhenIdNotAGuid_ThenValidationProblem()
    {
        var response = await _api.GetAsync("/policies/not-a-guid");

        await ProblemAssert.IsValidationProblemAsync(response, "id");
    }

    [Fact]
    public async Task Activate_WhenIdNotAGuid_ThenValidationProblem()
    {
        var response = await _api.PostAsync("/policies/not-a-guid/activate");

        await ProblemAssert.IsValidationProblemAsync(response, "id");
    }

    [Fact]
    public async Task Get_WhenMissing_ThenNotFoundWithCode()
    {
        var response = await _api.GetAsync($"/policies/{Guid.NewGuid()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, "Policy.NotFound");
    }
}
