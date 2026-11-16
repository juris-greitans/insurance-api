using Insurance.Api.Domain.Common;

namespace Insurance.Api.Domain.Policies;

public static class PolicyErrors
{
    public static readonly DomainError CustomerIdRequired =
        DomainError.Validation("Policy.CustomerIdRequired", "Customer id is required.");

    public static readonly DomainError InvalidProductType =
        DomainError.Validation("Policy.InvalidProductType", "Product type is not supported.");

    public static readonly DomainError InvalidPeriod =
        DomainError.Validation("Policy.InvalidPeriod", "End date must be after start date.");

    public static readonly DomainError InvalidPremium = DomainError.Validation(
        "Policy.InvalidPremium",
        $"Premium must be greater than zero with at most {Money.MaxDecimalPlaces} decimal places.");

    public static readonly DomainError NotDraft =
        DomainError.Conflict("Policy.NotDraft", "Only a draft policy can be activated.");

    public static readonly DomainError OverlappingActivePolicy = DomainError.Conflict(
        "Policy.OverlappingActivePolicy",
        "The customer already has an active policy of the same product type with an overlapping period.");
}
