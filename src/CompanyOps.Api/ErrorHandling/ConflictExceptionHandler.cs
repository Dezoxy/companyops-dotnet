using CompanyOps.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CompanyOps.Api.ErrorHandling;

/// <summary>
/// Translates a <see cref="ConflictException"/> (the action conflicts with existing state, e.g. a
/// duplicate asset tag) into a 409 problem response — so a unique-constraint clash reads as a
/// client conflict, not a leaked 500. Other exceptions fall through to the default handler.
/// </summary>
internal sealed class ConflictExceptionHandler(IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ConflictException conflictException)
        {
            return false; // not ours — let the pipeline handle it
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = conflictException,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = conflictException.Message,
            },
        });
    }
}
