using System.ComponentModel.DataAnnotations;
using Insurance.Api.Domain.Policies;

namespace Insurance.Api.Controllers.Contracts;

/// <summary>
/// Value-type properties are nullable so that a missing field is a 400 instead of silently becoming its default.
/// </summary>
public sealed class CreatePolicyRequest
{
    [Required]
    public Guid? CustomerId { get; init; }

    [Required]
    public ProductType? ProductType { get; init; }

    [Required]
    public DateOnly? StartDate { get; init; }

    [Required]
    public DateOnly? EndDate { get; init; }

    [Required]
    public decimal? Premium { get; init; }
}
