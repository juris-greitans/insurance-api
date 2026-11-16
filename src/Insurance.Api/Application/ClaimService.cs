using Insurance.Api.Application.Abstractions;
using Insurance.Api.Domain.Claims;
using Insurance.Api.Domain.Common;

namespace Insurance.Api.Application;

public sealed class ClaimService(IPolicyRepository policies, IClaimRepository claims, WriteLock writeLock)
{
    public Result<Claim> Create(Guid policyId, DateOnly incidentDate, decimal amountRequested) =>
        writeLock.Execute(() => CreateLocked(policyId, incidentDate, amountRequested));

    public Result<Claim> Decide(Guid id, ClaimDecision decision, string? reason) =>
        writeLock.Execute(() => DecideLocked(id, decision, reason));

    public Result<Claim> Get(Guid id)
    {
        var claim = claims.FindById(id);
        return claim is null ? ApplicationErrors.ClaimNotFound : claim;
    }

    private Result<Claim> CreateLocked(Guid policyId, DateOnly incidentDate, decimal amountRequested)
    {
        var policy = policies.FindById(policyId);
        if (policy is null)
        {
            return ApplicationErrors.ReferencedPolicyNotFound;
        }

        var result = Claim.Create(policy, incidentDate, amountRequested);
        if (result.IsSuccess)
        {
            claims.Save(result.Value);
        }

        return result;
    }

    private Result<Claim> DecideLocked(Guid id, ClaimDecision decision, string? reason)
    {
        var claim = claims.FindById(id);
        if (claim is null)
        {
            return ApplicationErrors.ClaimNotFound;
        }

        var result = claim.Decide(decision, reason);
        if (result.IsSuccess)
        {
            claims.Save(result.Value);
        }

        return result;
    }
}
