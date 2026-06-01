using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CompanyOps.Api.OpenApi;

/// <summary>
/// Sets <c>additionalProperties: false</c> on the request/response object schemas. This is honest
/// of the running API: it rejects unknown request fields (<c>UnmappedMemberHandling.Disallow</c>,
/// see Program.cs) and returns exactly the declared DTO shape. RFC 7807 problem objects are
/// intentionally extensible (they carry extension members such as <c>traceId</c>), so they are left
/// open. See docs/openapi-contract-plan.md (Phase 3).
/// </summary>
internal sealed class NoAdditionalPropertiesSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Leave the RFC 7807 problem-details family open (ProblemDetails / (Http)ValidationProblemDetails).
        if (typeof(ProblemDetails).IsAssignableFrom(context.JsonTypeInfo.Type))
        {
            return Task.CompletedTask;
        }

        // Object schemas only — enums and scalars have no properties and must stay untouched.
        if (schema.Properties is { Count: > 0 })
        {
            schema.AdditionalPropertiesAllowed = false;
        }

        return Task.CompletedTask;
    }
}
