using Insurance.Api.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Api.Controllers;

/// <summary>
/// The single place where <see cref="DomainError"/> is mapped to an HTTP status and ProblemDetails.
/// </summary>
/// <remarks>
/// 400 is declared as plain <see cref="ProblemDetails"/>: model validation adds an <c>errors</c> object,
/// domain validation adds a <c>code</c> instead.
/// </remarks>
[ApiController]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
public abstract class ApiControllerBase : ControllerBase
{
    public const string ProblemJson = "application/problem+json";

    public const string ErrorCodeExtension = "code";

    protected ActionResult<TResponse> OkOrProblem<T, TResponse>(Result<T> result, Func<T, TResponse> map)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);

        return result.IsSuccess ? Ok(map(result.Value)) : ProblemFor(result.Error!);
    }

    /// <summary>201 with a Location header pointing at <paramref name="getActionName"/>, or a problem.</summary>
    protected ActionResult<TResponse> CreatedOrProblem<T, TResponse>(
        Result<T> result,
        string getActionName,
        Func<T, TResponse> map,
        Func<T, Guid> getId)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(getId);

        if (result.IsFailure)
        {
            return ProblemFor(result.Error!);
        }

        return CreatedAtAction(getActionName, new { id = getId(result.Value) }, map(result.Value));
    }

    protected ObjectResult ProblemFor(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var statusCode = StatusCodeFor(error.Type);
        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, statusCode, detail: error.Message);
        problem.Extensions[ErrorCodeExtension] = error.Code;

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { ProblemJson },
        };
    }

    internal static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown error type."),
    };
}
