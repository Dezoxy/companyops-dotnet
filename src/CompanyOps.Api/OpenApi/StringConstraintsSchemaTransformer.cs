using CompanyOps.Domain.Assets;
using CompanyOps.Domain.Requests;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CompanyOps.Api.OpenApi;

/// <summary>
/// Adds the string constraints the API actually enforces, so the contract is honest:
/// <list type="bullet">
/// <item><c>uuid</c> / <c>date-time</c> formatted strings get the matching <c>pattern</c> +
/// <c>maxLength</c> (these values genuinely conform to the format);</item>
/// <item>free-text fields get the <c>maxLength</c> the Domain enforces (title, description, tag,
/// name, comment body) — by property name, since the same caps apply on request and response.</item>
/// </list>
/// Deliberately NOT added: free-text <c>pattern</c>s (the code doesn't constrain the character set —
/// claiming a regex would fail conformance, the lesson from the scan session). See
/// docs/openapi-contract-plan.md (Phase 4).
/// </summary>
internal sealed class StringConstraintsSchemaTransformer : IOpenApiSchemaTransformer
{
    private const string UuidPattern = "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$";
    private const string DateTimePattern = "^\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}(\\.\\d+)?(Z|[+-]\\d{2}:\\d{2})$";

    // Free-text caps enforced in the Domain (Request.Create / Asset.Register / Comment.Create).
    private static readonly Dictionary<string, int> TextMaxLengths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["title"] = Request.TitleMaxLength,
        ["description"] = Request.DescriptionMaxLength,
        ["tag"] = Asset.TagMaxLength,
        ["name"] = Asset.NameMaxLength,
        ["body"] = Comment.BodyMaxLength,
    };

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        switch (schema.Format)
        {
            case "uuid":
                schema.Pattern ??= UuidPattern;
                schema.MaxLength ??= 36;
                return Task.CompletedTask;
            case "date-time":
                schema.Pattern ??= DateTimePattern;
                schema.MaxLength ??= 64;
                return Task.CompletedTask;
        }

        var propertyName = context.JsonPropertyInfo?.Name;
        if (propertyName is not null && TextMaxLengths.TryGetValue(propertyName, out var max) && schema.MaxLength is null)
        {
            schema.MaxLength = max;
        }

        return Task.CompletedTask;
    }
}
