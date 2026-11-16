using Insurance.Api.Domain.Common;

namespace Insurance.Api.Domain.Policies;

/// <summary>
/// Insurance policy. Immutable: state changes return a new instance.
/// The period is inclusive on both ends.
/// </summary>
public sealed record Policy
{
    private Policy(
        Guid id,
        Guid customerId,
        ProductType productType,
        DateOnly startDate,
        DateOnly endDate,
        decimal premium,
        PolicyStatus status)
    {
        Id = id;
        CustomerId = customerId;
        ProductType = productType;
        StartDate = startDate;
        EndDate = endDate;
        Premium = premium;
        Status = status;
    }

    public Guid Id { get; }

    public Guid CustomerId { get; }

    public ProductType ProductType { get; }

    public DateOnly StartDate { get; }

    public DateOnly EndDate { get; }

    public decimal Premium { get; }

    public PolicyStatus Status { get; private init; }

    public static Result<Policy> Create(
        Guid customerId,
        ProductType productType,
        DateOnly startDate,
        DateOnly endDate,
        decimal premium)
    {
        if (customerId == Guid.Empty)
        {
            return PolicyErrors.CustomerIdRequired;
        }

        if (!Enum.IsDefined(productType))
        {
            return PolicyErrors.InvalidProductType;
        }

        if (endDate <= startDate)
        {
            return PolicyErrors.InvalidPeriod;
        }

        if (!Money.IsValidPositiveAmount(premium))
        {
            return PolicyErrors.InvalidPremium;
        }

        return new Policy(Guid.NewGuid(), customerId, productType, startDate, endDate, premium, PolicyStatus.Draft);
    }

    /// <summary>
    /// Activates a draft policy.
    /// </summary>
    /// <param name="customerPolicies">
    /// The customer's existing policies, used for the overlap rule. May include this policy and policies in any status.
    /// </param>
    public Result<Policy> Activate(IEnumerable<Policy> customerPolicies)
    {
        ArgumentNullException.ThrowIfNull(customerPolicies);

        if (Status != PolicyStatus.Draft)
        {
            return PolicyErrors.NotDraft;
        }

        var overlaps = customerPolicies.Any(other =>
            other.Id != Id
            && other.CustomerId == CustomerId
            && other.Status == PolicyStatus.Active
            && other.ProductType == ProductType
            && Overlaps(other));

        if (overlaps)
        {
            return PolicyErrors.OverlappingActivePolicy;
        }

        return this with { Status = PolicyStatus.Active };
    }

    public bool Covers(DateOnly date) => StartDate <= date && date <= EndDate;

    private bool Overlaps(Policy other) => StartDate <= other.EndDate && other.StartDate <= EndDate;
}
