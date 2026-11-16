using Insurance.Api.Application;
using Insurance.Api.Controllers.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Api.Controllers;

[Route("customers")]
public sealed class CustomersController(CustomerService customers) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    public ActionResult<CustomerResponse> Create(CreateCustomerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return CreatedOrProblem(customers.Create(request.FullName), nameof(Get), CustomerResponse.From, c => c.Id);
    }

    [HttpGet("{id}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    public ActionResult<CustomerResponse> Get(Guid id) => OkOrProblem(customers.Get(id), CustomerResponse.From);
}
