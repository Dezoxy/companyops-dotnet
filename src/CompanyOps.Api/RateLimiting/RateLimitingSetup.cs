using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CompanyOps.Api.RateLimiting;

/// <summary>
/// A global rate limit as a coarse DoS/abuse guard — the API is the authority, this just keeps it
/// from being flooded. Partitioned by the authenticated user (<c>sub</c>), falling back to the
/// client IP for anonymous callers; health probes are exempt. Limits are config-bound
/// (<see cref="RateLimitingOptions"/>) so they're tunable per environment. Rejections are 429 with
/// a <c>Retry-After</c> header. Runs after authentication (so the partition can key on the user)
/// but before authorization and the endpoints.
/// </summary>
public static class RateLimitingSetup
{
    public static IServiceCollection AddCompanyOpsRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = options.WindowSeconds.ToString(CultureInfo.InvariantCulture);
                return ValueTask.CompletedTask;
            };

            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Liveness/readiness probes are polled constantly — never rate-limit them.
                if (context.Request.Path.StartsWithSegments("/health"))
                {
                    return RateLimitPartition.GetNoLimiter("health");
                }

                var partitionKey = context.User.FindFirstValue("sub")
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimit,
                    Window = TimeSpan.FromSeconds(options.WindowSeconds),
                    QueueLimit = 0,
                });
            });
        });

        return services;
    }
}
