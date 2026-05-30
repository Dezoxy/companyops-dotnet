namespace CompanyOps.Application.ExternalSystems;

/// <summary>Result of committing budget in the external Finance system.</summary>
public sealed record BudgetCommitment(Guid CommitmentId);

/// <summary>
/// Port to the external Finance system. Called (from the Worker) when a request is
/// approved, to commit/reserve its budget. Throws if the call fails after the
/// resilience pipeline is exhausted — the caller treats that as a transient failure
/// to retry out-of-band (ADR 0008).
/// </summary>
public interface IFinanceGateway
{
    Task<BudgetCommitment> CommitBudgetAsync(Guid requestId, Guid departmentId, CancellationToken cancellationToken = default);
}
