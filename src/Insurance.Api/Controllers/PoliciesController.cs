using Insurance.Api.Application;
using Insurance.Api.Controllers.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Api.Controllers;

[Route("policies")]
public sealed class PoliciesController(PolicyService policies) : ApiControllerBase
{
    /// <summary>Creates a policy in Draft status.</summary>
    [HttpPost]
    [ProducesResponseType<PolicyResponse>(StatusCodes.Status201Created)]
    public ActionResult<PolicyResponse> Create(CreatePolicyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // [Required] guarantees the values are present.
        var result = policies.Create(
            request.CustomerId!.Value,
            request.ProductType!.Value,
            request.StartDate!.Value,
            request.EndDate!.Value,
            request.Premium!.Value);

        return CreatedOrProblem(result, nameof(Get), PolicyResponse.From, p => p.Id);
    }

    /// <summary>Activates a Draft policy. Fails with 409 if it is not a Draft or overlaps an active policy.</summary>
    [HttpPost("{id}/activate")]
    [ProducesResponseType<PolicyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemJson)]
    public ActionResult<PolicyResponse> Activate(Guid id) => OkOrProblem(policies.Activate(id), PolicyResponse.From);

    [HttpGet("{id}")]
    [ProducesResponseType<PolicyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    public ActionResult<PolicyResponse> Get(Guid id) => OkOrProblem(policies.Get(id), PolicyResponse.From);
}
