using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CompanyOps.Api.OpenApi;

/// <summary>
/// Documents the standard error responses every operation can actually produce, so the contract is
/// complete and honest:
/// <list type="bullet">
/// <item><c>default</c> — the RFC 7807 problem response for unhandled errors;</item>
/// <item><c>429</c> — the rate limiter is real (<c>AddCompanyOpsRateLimiting</c>);</item>
/// <item><c>415</c> — JSON-only: a body-bearing request with an unsupported content type is rejected
/// (added only to operations that take a request body).</item>
/// </list>
/// Not added: <c>406</c> (the API doesn't do content negotiation today) and <c>maxItems</c>
/// (the lists aren't paginated yet) — neither is enforced, so claiming them would be dishonest.
/// See docs/openapi-contract-plan.md (Phase 4).
/// </summary>
internal sealed class StandardErrorResponsesTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        OpenApiResponse Problem(string description) => new()
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/problem+json"] = new() { Schema = new OpenApiSchemaReference("ProblemDetails", document) },
            },
        };

        foreach (var pathItem in document.Paths.Values)
        {
            if (pathItem.Operations is null)
            {
                continue;
            }

            foreach (var (_, operation) in pathItem.Operations)
            {
                operation.Responses ??= new OpenApiResponses();
                operation.Responses.TryAdd("default", Problem("Unexpected error."));
                operation.Responses.TryAdd("429", Problem("Rate limit exceeded — retry after backing off."));

                if (operation.RequestBody is not null)
                {
                    operation.Responses.TryAdd("415", Problem("Unsupported media type — the API accepts application/json."));
                }
            }
        }

        return Task.CompletedTask;
    }
}
