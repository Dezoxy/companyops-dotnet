using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CompanyOps.Api.Auth;

/// <summary>
/// Wires Keycloak OIDC bearer authentication (the API is a resource server validating
/// JWTs) and the role-based authorization policies. See docs/security.md.
/// </summary>
public static class AuthSetup
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        var authority = configuration["Keycloak:Authority"]
            ?? throw new InvalidOperationException("Keycloak:Authority is not configured. Set it via configuration/environment.");
        var audience = configuration["Keycloak:Audience"]
            ?? throw new InvalidOperationException("Keycloak:Audience is not configured. Set it via configuration/environment.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                // Keycloak is served over plain HTTP locally; require HTTPS metadata everywhere else.
                options.RequireHttpsMetadata = !isDevelopment;

                // Keep original claim names (sub, department, realm_access) instead of the
                // legacy SOAP-style remapping, so claim lookups are predictable.
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = "preferred_username";
                options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidateLifetime = true;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;

                // Keycloak nests realm roles under realm_access.roles; flatten them to
                // standard Role claims so [Authorize(Roles=…)] / RequireRole work.
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            FlattenRealmRoles(identity);
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    public static IServiceCollection AddCompanyOpsAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.CreateRequests, p => p.RequireRole(Roles.Employee))
            .AddPolicy(Policies.SubmitRequests, p => p.RequireRole(Roles.Employee))
            .AddPolicy(Policies.DecideRequests, p => p.RequireRole(Roles.Manager, Roles.Finance))
            .AddPolicy(Policies.FulfillRequests, p => p.RequireRole(Roles.ItAdmin))
            // Any participant may comment; the read-only Auditor (Auditor-only) is excluded.
            .AddPolicy(Policies.CommentOnRequests, p => p.RequireRole(Roles.Employee, Roles.Manager, Roles.Finance, Roles.ItAdmin))
            .AddPolicy(Policies.ReadAuditLog, p => p.RequireRole(Roles.Auditor))
            .AddPolicy(Policies.ManageAssets, p => p.RequireRole(Roles.ItAdmin))
            .AddPolicy(Policies.ReadAssets, p => p.RequireRole(Roles.ItAdmin, Roles.Auditor))
            // Aggregate analytics for the oversight roles; plain Employees are excluded.
            .AddPolicy(Policies.ReadReports, p => p.RequireRole(Roles.Manager, Roles.Finance, Roles.ItAdmin, Roles.Auditor))
            // Operational integration status is for operators (IT Admin) + oversight (Auditor) only.
            .AddPolicy(Policies.ReadIntegrations, p => p.RequireRole(Roles.ItAdmin, Roles.Auditor));

        return services;
    }

    private static void FlattenRealmRoles(ClaimsIdentity identity)
    {
        var realmAccess = identity.FindFirst("realm_access")?.Value;
        if (string.IsNullOrEmpty(realmAccess))
        {
            return;
        }

        using var doc = JsonDocument.Parse(realmAccess);
        if (!doc.RootElement.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var role in roles.EnumerateArray())
        {
            if (role.GetString() is { Length: > 0 } name)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, name));
            }
        }
    }
}
