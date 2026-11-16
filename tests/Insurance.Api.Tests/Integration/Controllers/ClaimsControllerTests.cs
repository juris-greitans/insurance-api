using System.Net;
using Insurance.Api.Controllers.Contracts;
using Insurance.Api.Domain.Claims;
using Insurance.Api.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Insurance.Api.Tests.Integration.Controllers;

public class ClaimsControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private Task<HttpResponseMessage> DecideAsync(Guid claimId, string decision, string? decisionReason = null) =>
        _api.PostAsync($"/claims/{claimId}/decide", new { decision, decisionReason });

    private async Task<ClaimResponse> NewClaimAsync() =>
        await _api.CreateClaimAsync((await _api.CreateActivePolicyAsync()).Id);

    [Fact]
    public async Task Create_WhenValid_ThenReturnsCreatedNewClaimWithLocation()
    {
        var policy = await _api.CreateActivePolicyAsync();

        var response = await _api.PostAsync(
            "/claims",
            new { policyId = policy.Id, incidentDate = "2026-03-15", amountRequested = 1234.56m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var claim = await ApiTestClient.ReadSuccessAsync<ClaimResponse>(response);
        Assert.Equal(
            new ClaimResponse(claim.Id, policy.Id, new DateOnly(2026, 3, 15), 1234.56m, ClaimStatus.New, null),
            claim);
        Assert.Equal($"/claims/{claim.Id}", response.Headers.Location?.AbsolutePath, ignoreCase: true);
    }

    [Theory]
    [InlineData("2026-01-01")]
    [InlineData("2026-12-31")]
    public async Task Create_WhenIncidentOnPolicyBoundary_ThenCreated(string incidentDate)
    {
        var policy = await _api.CreateActivePolicyAsync();

        var response = await _api.PostAsync("/claims", new { policyId = policy.Id, incidentDate, amountRequested = 1m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("2025-12-31")]
    [InlineData("2027-01-01")]
    public async Task Create_WhenIncidentOutsidePolicyPeriod_ThenConflictWithCode(string incidentDate)
    {
        var policy = await _api.CreateActivePolicyAsync();

        var response = await _api.PostAsync("/claims", new { policyId = policy.Id, incidentDate, amountRequested = 1m });

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, "Claim.IncidentOutsidePolicyPeriod");
    }

    [Fact]
    public async Task Create_WhenPolicyIsDraft_ThenConflictWithCode()
    {
        var customer = await _api.CreateCustomerAsync();
        var draft = await _api.CreatePolicyAsync(customer.Id);

        var response = await _api.PostAsync(
            "/claims",
            new { policyId = draft.Id, incidentDate = "2026-06-15", amountRequested = 1m });

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, "Claim.PolicyNotActive");
    }

    [Fact]
    public async Task Create_WhenPolicyMissing_ThenBadRequestWithCode()
    {
        var response = await _api.PostAsync(
            "/claims",
            new { policyId = Guid.NewGuid(), incidentDate = "2026-06-15", amountRequested = 1m });

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest, "Claim.PolicyNotFound");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("1.005")]
    public async Task Create_WhenAmountInvalid_ThenBadRequestWithCode(string amount)
    {
        var policy = await _api.CreateActivePolicyAsync();

        var response = await _api.PostRawAsync(
            "/claims",
            $$"""{"policyId":"{{policy.Id}}","incidentDate":"2026-06-15","amountRequested":{{amount}}}""");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest, "Claim.InvalidAmount");
    }

    [Theory]
    [InlineData("policyId")]
    [InlineData("incidentDate")]
    [InlineData("amountRequested")]
    public async Task Create_WhenFieldMissing_ThenValidationProblem(string missingField)
    {
        var policy = await _api.CreateActivePolicyAsync();
        var fields = new Dictionary<string, object>
        {
            ["policyId"] = policy.Id,
            ["incidentDate"] = "2026-06-15",
            ["amountRequested"] = 1m,
        };
        fields.Remove(missingField);

        var response = await _api.PostAsync("/claims", fields);

        await ProblemAssert.IsValidationProblemAsync(response, missingField);
    }

    [Fact]
    public async Task Decide_WhenApproving_ThenReturnsApprovedClaim()
    {
        var claim = await NewClaimAsync();

        var response = await DecideAsync(claim.Id, "Approve");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var decided = await ApiTestClient.ReadSuccessAsync<ClaimResponse>(response);
        Assert.Equal(claim with { Status = ClaimStatus.Approved }, decided);
    }

    [Fact]
    public async Task Decide_WhenApprovingWithReason_ThenReasonIgnored()
    {
        var claim = await NewClaimAsync();

        var decided = await ApiTestClient.ReadSuccessAsync<ClaimResponse>(
            await DecideAsync(claim.Id, "Approve", "Looks fine"));

        Assert.Equal(ClaimStatus.Approved, decided.Status);
        Assert.Null(decided.DecisionReason);
    }

    [Fact]
    public async Task Decide_WhenRejectingWithReason_ThenReturnsRejectedClaimAndPersistsIt()
    {
        var claim = await NewClaimAsync();

        var response = await DecideAsync(claim.Id, "Reject", "Not covered");

        var decided = await ApiTestClient.ReadSuccessAsync<ClaimResponse>(response);
        Assert.Equal(claim with { Status = ClaimStatus.Rejected, DecisionReason = "Not covered" }, decided);
        var fetched = await ApiTestClient.ReadSuccessAsync<ClaimResponse>(await _api.GetAsync($"/claims/{claim.Id}"));
        Assert.Equal(decided, fetched);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Decide_WhenRejectingWithoutReason_ThenBadRequestWithCode(string? reason)
    {
        var claim = await NewClaimAsync();

        var response = await DecideAsync(claim.Id, "Reject", reason);

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest, "Claim.DecisionReasonRequired");
    }

    [Fact]
    public async Task Decide_WhenReasonTooLong_ThenBadRequestWithCode()
    {
        var claim = await NewClaimAsync();

        var response = await DecideAsync(claim.Id, "Reject", new string('r', 1001));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest, "Claim.DecisionReasonTooLong");
    }

    [Theory]
    [InlineData("Approve")]
    [InlineData("Reject")]
    public async Task Decide_WhenAlreadyDecided_ThenConflictWithCode(string secondDecision)
    {
        var claim = await NewClaimAsync();
        await ApiTestClient.ReadSuccessAsync<ClaimResponse>(await DecideAsync(claim.Id, "Approve"));

        var response = await DecideAsync(claim.Id, secondDecision, "Reason");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, "Claim.AlreadyDecided");
    }

    [Fact]
    public async Task Decide_WhenMissing_ThenNotFoundWithCode()
    {
        var response = await DecideAsync(Guid.NewGuid(), "Approve");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, "Claim.NotFound");
    }

    [Fact]
    public async Task Decide_WhenDecisionUnknown_ThenValidationProblem()
    {
        var claim = await NewClaimAsync();

        var response = await DecideAsync(claim.Id, "Maybe");

        await ProblemAssert.IsValidationProblemAsync(response, "decision");
    }

    [Fact]
    public async Task Decide_WhenDecisionSentAsNumber_ThenValidationProblem()
    {
        var claim = await NewClaimAsync();

        var response = await _api.PostRawAsync($"/claims/{claim.Id}/decide", "{\"decision\":1,\"decisionReason\":\"Reason\"}");

        await ProblemAssert.IsValidationProblemAsync(response, "decision");
        var stored = await ApiTestClient.ReadSuccessAsync<ClaimResponse>(await _api.GetAsync($"/claims/{claim.Id}"));
        Assert.Equal(ClaimStatus.New, stored.Status);
    }

    [Theory]
    [InlineData("Approve, Reject")]
    [InlineData("approve")]
    [InlineData(" Reject")]
    public async Task Decide_WhenDecisionNotExactName_ThenValidationProblemAndClaimStaysNew(string decision)
    {
        var claim = await NewClaimAsync();

        var response = await DecideAsync(claim.Id, decision, "Reason");

        await ProblemAssert.IsValidationProblemAsync(response, "decision");
        var stored = await ApiTestClient.ReadSuccessAsync<ClaimResponse>(await _api.GetAsync($"/claims/{claim.Id}"));
        Assert.Equal(ClaimStatus.New, stored.Status);
    }

    [Fact]
    public async Task Decide_WhenIdNotAGuid_ThenValidationProblem()
    {
        var response = await _api.PostAsync("/claims/not-a-guid/decide", new { decision = "Approve" });

        await ProblemAssert.IsValidationProblemAsync(response, "id");
    }

    [Fact]
    public async Task Get_WhenIdNotAGuid_ThenValidationProblem()
    {
        var response = await _api.GetAsync("/claims/not-a-guid");

        await ProblemAssert.IsValidationProblemAsync(response, "id");
    }

    [Fact]
    public async Task Decide_WhenDecisionMissing_ThenValidationProblem()
    {
        var claim = await NewClaimAsync();

        var response = await _api.PostAsync($"/claims/{claim.Id}/decide", new { decisionReason = "Reason" });

        await ProblemAssert.IsValidationProblemAsync(response, "Decision");
    }

    [Fact]
    public async Task Get_WhenMissing_ThenNotFoundWithCode()
    {
        var response = await _api.GetAsync($"/claims/{Guid.NewGuid()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, "Claim.NotFound");
    }
}
