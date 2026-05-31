namespace CompanyOps.Api.RateLimiting;

/// <summary>
/// Rate-limit settings, bound from the "RateLimiting" config section. A coarse per-caller flood
/// guard — generous by default; tune per environment (tests set it high so the suite isn't limited).
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Requests allowed per <see cref="WindowSeconds"/> window, per caller partition.</summary>
    public int PermitLimit { get; init; } = 120;

    public int WindowSeconds { get; init; } = 60;
}
