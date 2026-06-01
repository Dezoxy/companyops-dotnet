using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CompanyOps.Api.ErrorHandling;

/// <summary>
/// Translates a FluentValidation <see cref="ValidationException"/> (input that failed an
/// Application-boundary validator) into a 400 problem response carrying per-field error
/// messages. Other exceptions fall through so internal errors are never leaked to clients.
/// </summary>
internal sealed class ValidationExceptionHandler(IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false; // not ours — let the pipeline handle it
        }

        // Group failures by property so the client gets one entry per field (RFC 7807 "errors").
        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = validationException,
            ProblemDetails = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
            },
        });
    }
}
