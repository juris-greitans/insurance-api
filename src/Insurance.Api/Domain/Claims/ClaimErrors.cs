using Insurance.Api.Domain.Common;

namespace Insurance.Api.Domain.Claims;

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
