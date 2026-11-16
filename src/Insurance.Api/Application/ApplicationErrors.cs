using Insurance.Api.Domain.Common;

namespace Insurance.Api.Application;

/// <summary>
/// Errors for missing resources. A missing resource in the route is 404 (NotFound);
/// a missing resource referenced from a request body is 400 (Validation).
/// </summary>
public static class ApplicationErrors
{
    public static readonly DomainError CustomerNotFound =
        DomainError.NotFound("Customer.NotFound", "Customer was not found.");

    public static readonly DomainError PolicyNotFound =
        DomainError.NotFound("Policy.NotFound", "Policy was not found.");

    public static readonly DomainError ClaimNotFound =
        DomainError.NotFound("Claim.NotFound", "Claim was not found.");

    public static readonly DomainError ReferencedCustomerNotFound =
        DomainError.Validation("Policy.CustomerNotFound", "The referenced customer does not exist.");

    public static readonly DomainError ReferencedPolicyNotFound =
        DomainError.Validation("Claim.PolicyNotFound", "The referenced policy does not exist.");
}
