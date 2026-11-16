using Insurance.Api.Application.Abstractions;
using Insurance.Api.Domain.Claims;

namespace Insurance.Api.Infrastructure;

public sealed class InMemoryClaimRepository() : InMemoryRepository<Claim>(c => c.Id), IClaimRepository;
