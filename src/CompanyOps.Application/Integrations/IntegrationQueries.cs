using CompanyOps.Application.Abstractions;

namespace CompanyOps.Application.Integrations;

/// <summary>
/// Read use-case for the Integrations screen (Phase 19): a snapshot of the outbox + worker
/// pipeline. Thin — delegates to the read port (the seam for any future filtering / paging).
/// </summary>
public sealed record GetIntegrationStatusQuery;

public sealed class GetIntegrationStatusHandler(IIntegrationStatusStore integrations)
{
    public Task<IntegrationStatusDto> HandleAsync(GetIntegrationStatusQuery query, CancellationToken cancellationToken = default) =>
        integrations.GetStatusAsync(cancellationToken);
}
