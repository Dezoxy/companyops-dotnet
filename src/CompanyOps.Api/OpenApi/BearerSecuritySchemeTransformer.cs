using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CompanyOps.Api.OpenApi;

/// <summary>
/// Declares the things .NET's built-in <c>AddOpenApi</c> cannot infer from the app, so the
/// generated contract is security-accurate:
/// <list type="bullet">
/// <item>the Bearer/JWT (Keycloak) security scheme + a document-wide security requirement —
/// without it the contract would describe an unauthenticated API (every endpoint is
/// <c>[Authorize]</c>);</item>
/// <item>the production <c>servers</c> entry (HTTPS via the Traefik edge) — without an https
/// server the token reads as "sent over cleartext" and the security score is zeroed.</item>
/// </list>
/// See docs/openapi-contract-plan.md (Phase 2; servers folded in from Phase 3 since the security
/// score depends on it).
/// </summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "OIDC access token issued by Keycloak (realm companyops, audience companyops-api).",
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = scheme;

        // Apply globally — there are no anonymous endpoints (every controller is [Authorize]).
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, document)] = [],
        });

        // HTTPS server — the API is served behind the Traefik TLS edge in production. Declaring it
        // clears the cleartext-bearer finding (a token over http:// is interceptable).
        document.Servers =
        [
            new OpenApiServer { Url = "https://companyops.toomhorvath.com", Description = "Production (behind Traefik TLS)" },
        ];

        return Task.CompletedTask;
    }
}
