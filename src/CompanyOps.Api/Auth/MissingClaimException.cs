namespace CompanyOps.Api.Auth;

/// <summary>
/// The authenticated principal is missing a claim the API requires to act on its behalf
/// (e.g. <c>sub</c> or <c>department</c>). The token is valid — it passed signature/issuer/
/// audience/expiry validation — but is insufficient for the operation: a realm/mapper
/// misconfiguration, not a server fault. Mapped to 403 by <c>MissingClaimExceptionHandler</c>,
/// never surfaced as a leaked 500.
/// </summary>
public sealed class MissingClaimException(string claim)
    : Exception($"The authenticated principal is missing the required '{claim}' claim.")
{
    public string Claim { get; } = claim;
}
