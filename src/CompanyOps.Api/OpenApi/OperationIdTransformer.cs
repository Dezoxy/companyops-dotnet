using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CompanyOps.Api.OpenApi;

/// <summary>
/// Assigns a stable, meaningful <c>operationId</c> to every operation. .NET's <c>AddOpenApi</c>
/// does not emit operationIds for controller actions, but they are essential: client generators
/// name methods after them and tooling (the 42Crunch scan, Postman, etc.) keys on them. Mapped by
/// HTTP method + route so the ids stay stable regardless of how the action methods are named.
/// See docs/openapi-contract-plan.md (Phase 6).
/// </summary>
internal sealed class OperationIdTransformer : IOpenApiOperationTransformer
{
    private static readonly Dictionary<string, string> Ids = new()
    {
        ["POST /requests"] = "createRequest",
        ["GET /requests"] = "listRequests",
        ["GET /requests/{id}"] = "getRequestById",
        ["POST /requests/{id}/submit"] = "submitRequest",
        ["POST /requests/{id}/cancel"] = "cancelRequest",
        ["POST /requests/{id}/approve"] = "approveRequest",
        ["POST /requests/{id}/reject"] = "rejectRequest",
        ["POST /requests/{id}/fulfill"] = "fulfillRequest",
        ["POST /requests/{id}/comments"] = "addComment",
        ["GET /requests/{id}/comments"] = "listComments",
        ["GET /assets"] = "listAssets",
        ["POST /assets"] = "registerAsset",
        ["GET /assets/{id}"] = "getAssetById",
        ["GET /assets/{id}/history"] = "getAssetHistory",
        ["POST /assets/{id}/assign"] = "assignAsset",
        ["POST /assets/{id}/reclaim"] = "reclaimAsset",
        ["POST /assets/{id}/repair"] = "sendAssetToRepair",
        ["POST /assets/{id}/return-from-repair"] = "returnAssetFromRepair",
        ["POST /assets/{id}/retire"] = "retireAsset",
        ["GET /reports/requests"] = "getRequestReport",
        ["GET /reports/assets"] = "getAssetReport",
        ["GET /audit-logs"] = "listAuditLogs",
        ["GET /integrations/status"] = "getIntegrationStatus",
    };

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var method = context.Description.HttpMethod?.ToUpperInvariant();
        var path = "/" + (context.Description.RelativePath ?? string.Empty).TrimEnd('/');
        if (method is not null && Ids.TryGetValue($"{method} {path}", out var id))
        {
            operation.OperationId = id;
        }

        return Task.CompletedTask;
    }
}
