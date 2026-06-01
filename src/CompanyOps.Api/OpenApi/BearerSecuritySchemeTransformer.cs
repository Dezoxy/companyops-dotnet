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
/// server the token reads as "sent over cleartext" and the security score is zeroed. Applied
/// <b>only to the build-time/canonical document</b>; the runtime dev <c>/openapi</c> + Scalar keep
/// relative servers so local "try it" / client generation targets localhost, not production.</item>
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

        // HTTPS server — ONLY for the build-time/canonical contract. It clears the cleartext-bearer
        // finding (a token over http:// is interceptable). It must NOT be set on the runtime dev
        // document: that would make local Scalar / a client generated from localhost's /openapi
        // target production instead of the running localhost API. At runtime, leave servers unset
        // (relative → the document's own origin, i.e. localhost).
        if (BuildTimeOpenApi.IsGenerating)
        {
            document.Servers =
            [
                new OpenApiServer { Url = "https://companyops.toomhorvath.com", Description = "Production (behind Traefik TLS)" },
            ];
        }

        return Task.CompletedTask;
    }
}
