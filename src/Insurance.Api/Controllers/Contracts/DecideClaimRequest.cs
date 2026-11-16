using System.ComponentModel.DataAnnotations;
using Insurance.Api.Domain.Claims;

namespace Insurance.Api.Controllers.Contracts;

public sealed class DecideClaimRequest
{
    [Required]
    public ClaimDecision? Decision { get; init; }

    /// <summary>Required when rejecting; ignored when approving.</summary>
    public string? DecisionReason { get; init; }
}
