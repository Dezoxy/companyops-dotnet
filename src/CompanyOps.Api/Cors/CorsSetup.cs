namespace CompanyOps.Api.Cors;

/// <summary>
/// CORS for the Angular SPA. Allowed origins come from config (<c>Cors:AllowedOrigins</c>) —
/// the dev origin (http://localhost:4200) in Development, the deployed SPA origin via env in
/// production, and empty where the SPA is served same-origin behind the edge. The SPA sends a
/// Bearer token (not cookies), so credentials aren't enabled.
/// </summary>
public static class CorsSetup
{
    public const string PolicyName = "spa";

    public static IServiceCollection AddCompanyOpsCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        return services.AddCors(options =>
            options.AddPolicy(PolicyName, policy =>
            {
                if (origins.Length > 0)
                {
                    // The API is GET (reads) + POST (business actions) only — no CRUD verbs.
                    policy.WithOrigins(origins).AllowAnyHeader().WithMethods("GET", "POST");
                }
            }));
    }
}
