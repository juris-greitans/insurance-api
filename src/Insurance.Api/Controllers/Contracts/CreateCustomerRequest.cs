namespace Insurance.Api.Controllers.Contracts;

public sealed class CreateCustomerRequest
{
    /// <summary>Validated by the domain (required, not whitespace-only, max 250 after trimming).</summary>
    public string? FullName { get; init; }
}
