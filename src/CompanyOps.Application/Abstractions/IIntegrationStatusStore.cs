using CompanyOps.Application.Integrations;

namespace CompanyOps.Application.Abstractions;

/// <summary>
/// Read port for the async-integration pipeline's status (Phase 19). The outbox and
/// processed-message tables are Infrastructure plumbing (ADR 0007/0008), not domain concepts;
/// this port exposes a read-only operational snapshot of them without leaking those types upward.
/// </summary>
public interface IIntegrationStatusStore
{
    Task<IntegrationStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
}
