using Insurance.Api.Domain.Claims;

namespace Insurance.Api.Controllers.Contracts;

public sealed record ClaimResponse(
    Guid Id,
    Guid PolicyId,
    DateOnly IncidentDate,
    decimal AmountRequested,
    ClaimStatus Status,
    string? DecisionReason)
{
    public static ClaimResponse From(Claim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        return new ClaimResponse(
            claim.Id,
            claim.PolicyId,
            claim.IncidentDate,
            claim.AmountRequested,
            claim.Status,
            claim.DecisionReason);
    }
}
