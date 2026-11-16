using System.ComponentModel.DataAnnotations;

namespace Insurance.Api.Controllers.Contracts;

/// <summary>
/// Value-type properties are nullable so that a missing field is a 400 instead of silently becoming its default.
/// </summary>
public sealed class CreateClaimRequest
{
    [Required]
    public Guid? PolicyId { get; init; }

    [Required]
    public DateOnly? IncidentDate { get; init; }

    [Required]
    public decimal? AmountRequested { get; init; }
}
