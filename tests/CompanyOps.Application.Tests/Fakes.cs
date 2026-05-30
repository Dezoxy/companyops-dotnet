using CompanyOps.Application.Abstractions;
using CompanyOps.Application.IntegrationEvents;
using CompanyOps.Domain.Auditing;
using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Tests;

// Hand-written fakes (no mocking library): an in-memory repository and capturing
// write-ports let handler tests assert orchestration (saved / audited / enqueued)
// in milliseconds, with no database.

internal sealed class FakeRequestRepository : IRequestRepository
{
    private readonly Dictionary<Guid, Request> _store = [];

    public void Seed(Request request) => _store[request.Id] = request;
    public IReadOnlyDictionary<Guid, Request> Store => _store;

    public void Add(Request request) => _store[request.Id] = request;

    public Task<Request?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public Task<Request?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public Task<IReadOnlyList<Request>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Request>>([.. _store.Values]);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(0);
    }
}

internal sealed class CapturingAuditLogger : IAuditLogger
{
    public List<AuditLog> Entries { get; } = [];

    public void Add(AuditLog entry) => Entries.Add(entry);
}

internal sealed class CapturingEventPublisher : IIntegrationEventPublisher
{
    public List<IIntegrationEvent> Events { get; } = [];

    public void Enqueue(IIntegrationEvent integrationEvent) => Events.Add(integrationEvent);
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
