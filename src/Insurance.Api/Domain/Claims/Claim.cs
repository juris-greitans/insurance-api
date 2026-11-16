using Insurance.Api.Domain.Common;
using Insurance.Api.Domain.Policies;

namespace Insurance.Api.Domain.Claims;

public enum ClaimStatus
{
    New,
    Approved,
    Rejected,
}

public enum ClaimDecision
{
    Approve,
    Reject,
}

/// <summary>
/// Insurance claim. Immutable: state changes return a new instance.
/// </summary>
public sealed record Claim
{
    public const int DecisionReasonMaxLength = 1000;

    private Claim(Guid id, Guid policyId, DateOnly incidentDate, decimal amountRequested)
    {
        Id = id;
        PolicyId = policyId;
        IncidentDate = incidentDate;
        AmountRequested = amountRequested;
        Status = ClaimStatus.New;
    }

    public Guid Id { get; }

    public Guid PolicyId { get; }

    public DateOnly IncidentDate { get; }

    public decimal AmountRequested { get; }

    public ClaimStatus Status { get; private init; }

    /// <summary>Set only when <see cref="Status"/> is <see cref="ClaimStatus.Rejected"/>.</summary>
    public string? DecisionReason { get; private init; }

    public static Result<Claim> Create(Policy policy, DateOnly incidentDate, decimal amountRequested)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (!Money.IsValidPositiveAmount(amountRequested))
        {
            return ClaimErrors.InvalidAmount;
        }

        if (policy.Status != PolicyStatus.Active)
        {
            return ClaimErrors.PolicyNotActive;
        }

        if (!policy.Covers(incidentDate))
        {
            return ClaimErrors.IncidentOutsidePolicyPeriod;
        }

        return new Claim(Guid.NewGuid(), policy.Id, incidentDate, amountRequested);
    }

    /// <param name="decision">Approve or reject.</param>
    /// <param name="reason">Required when rejecting; ignored when approving.</param>
    public Result<Claim> Decide(ClaimDecision decision, string? reason)
    {
        if (!Enum.IsDefined(decision))
        {
            return ClaimErrors.InvalidDecision;
        }

        string? decisionReason = null;
        if (decision == ClaimDecision.Reject)
        {
            decisionReason = reason?.Trim();
            if (string.IsNullOrEmpty(decisionReason))
            {
                return ClaimErrors.DecisionReasonRequired;
            }

            if (decisionReason.Length > DecisionReasonMaxLength)
            {
                return ClaimErrors.DecisionReasonTooLong;
            }
        }

        if (Status != ClaimStatus.New)
        {
            return ClaimErrors.AlreadyDecided;
        }

        var status = decision == ClaimDecision.Approve ? ClaimStatus.Approved : ClaimStatus.Rejected;
        return this with { Status = status, DecisionReason = decisionReason };
    }
}

public static class ClaimErrors
{
    public static readonly DomainError InvalidAmount = DomainError.Validation(
        "Claim.InvalidAmount",
        $"Amount requested must be greater than zero with at most {Money.MaxDecimalPlaces} decimal places.");

    public static readonly DomainError PolicyNotActive =
        DomainError.Conflict("Claim.PolicyNotActive", "Claims can be created only for an active policy.");

    public static readonly DomainError IncidentOutsidePolicyPeriod =
        DomainError.Conflict("Claim.IncidentOutsidePolicyPeriod", "Incident date must be within the policy period.");

    public static readonly DomainError InvalidDecision =
        DomainError.Validation("Claim.InvalidDecision", "Decision is not supported.");

    public static readonly DomainError DecisionReasonRequired =
        DomainError.Validation("Claim.DecisionReasonRequired", "Decision reason is required when rejecting a claim.");

    public static readonly DomainError DecisionReasonTooLong = DomainError.Validation(
        "Claim.DecisionReasonTooLong",
        $"Decision reason must be at most {Claim.DecisionReasonMaxLength} characters.");

    public static readonly DomainError AlreadyDecided =
        DomainError.Conflict("Claim.AlreadyDecided", "Only a new claim can be decided.");
}
