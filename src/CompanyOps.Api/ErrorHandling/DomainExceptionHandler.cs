using CompanyOps.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CompanyOps.Api.ErrorHandling;

/// <summary>
/// Translates a <see cref="DomainException"/> (a broken business rule) into a
/// 400 problem response. Other exceptions fall through to the default handler so
/// internal errors are never leaked to clients.
/// </summary>
internal sealed class DomainExceptionHandler(IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
        {
            return false; // not ours — let the pipeline handle it
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = domainException,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Domain rule violation",
                Detail = domainException.Message,
            },
        });
    }
}
