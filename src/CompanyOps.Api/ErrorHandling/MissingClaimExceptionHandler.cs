using CompanyOps.Api.Auth;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CompanyOps.Api.ErrorHandling;

/// <summary>
/// Translates a <see cref="MissingClaimException"/> — a valid token that lacks a claim the API
/// needs (<c>sub</c> / <c>department</c>) — into a 403 problem response. The principal is
/// authenticated but insufficient for the operation, so it is a client/config condition (403),
/// not a leaked 500. Other exceptions fall through to the default handler.
/// </summary>
internal sealed class MissingClaimExceptionHandler(IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not MissingClaimException missingClaim)
        {
            return false; // not ours — let the pipeline handle it
        }

        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = missingClaim,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Insufficient principal",
                Detail = missingClaim.Message,
            },
        });
    }
}
