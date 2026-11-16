using Insurance.Api.Application;
using Insurance.Api.Controllers.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Api.Controllers;

[Route("claims")]
public sealed class ClaimsController(ClaimService claims) : ApiControllerBase
{
    /// <summary>Creates a New claim for an Active policy.</summary>
    [HttpPost]
    [ProducesResponseType<ClaimResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemJson)]
    public ActionResult<ClaimResponse> Create(CreateClaimRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // [Required] guarantees the values are present.
        var result = claims.Create(
            request.PolicyId!.Value,
            request.IncidentDate!.Value,
            request.AmountRequested!.Value);

        return CreatedOrProblem(result, nameof(Get), ClaimResponse.From, c => c.Id);
    }

    /// <summary>Approves or rejects a New claim. Rejecting requires a decision reason.</summary>
    [HttpPost("{id}/decide")]
    [ProducesResponseType<ClaimResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemJson)]
    public ActionResult<ClaimResponse> Decide(Guid id, DecideClaimRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return OkOrProblem(
            claims.Decide(id, request.Decision!.Value, request.DecisionReason),
            ClaimResponse.From);
    }

    [HttpGet("{id}")]
    [ProducesResponseType<ClaimResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    public ActionResult<ClaimResponse> Get(Guid id) => OkOrProblem(claims.Get(id), ClaimResponse.From);
}
