using Insurance.Api.Domain.Claims;

namespace Insurance.Api.Application.Abstractions;

public interface IClaimRepository
{
    Claim? FindById(Guid id);

    /// <summary>Inserts or replaces the claim with the same id.</summary>
    void Save(Claim claim);
}
