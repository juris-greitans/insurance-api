using Insurance.Api.Domain.Common;
using Insurance.Api.Domain.Policies;

namespace Insurance.Api.Domain.Claims;

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
