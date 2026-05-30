using System.Net.Http.Json;
using CompanyOps.Application.ExternalSystems;

namespace CompanyOps.Infrastructure.ExternalSystems;

/// <summary>
/// HTTP client for the external Finance system. Resilience (timeout + retry) is attached
/// to the named <see cref="HttpClient"/> in DI; this type just speaks the wire contract.
/// A non-success response throws (via EnsureSuccessStatusCode), which — once the
/// resilience pipeline is exhausted — surfaces to the caller as a transient failure.
/// </summary>
internal sealed class FinanceGateway(HttpClient httpClient) : IFinanceGateway
{
    public async Task<BudgetCommitment> CommitBudgetAsync(Guid requestId, Guid departmentId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/finance/commitments",
            new BudgetCommitRequest(requestId, departmentId),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<BudgetCommitResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Finance system returned an empty commitment response.");

        return new BudgetCommitment(body.CommitmentId);
    }

    private sealed record BudgetCommitRequest(Guid RequestId, Guid DepartmentId);
    private sealed record BudgetCommitResponse(Guid CommitmentId, string Status);
}
