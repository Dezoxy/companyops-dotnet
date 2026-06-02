using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace CompanyOps.Infrastructure.Persistence;

internal sealed class RequestRepository(AppDbContext dbContext) : IRequestRepository
{
    public void Add(Request request) => dbContext.Requests.Add(request);

    public Task<Request?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Requests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Request?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        // Tracked load (no AsNoTracking) so transitions persist. The owned ApprovalSteps
        // are auto-included by EF with the owner — no explicit Include needed.
        dbContext.Requests
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Request>> ListAsync(
        Guid? requesterId,
        Guid? departmentId,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        await Scoped(requesterId, departmentId)
            // Tie-break on Id so paging is deterministic when CreatedAtUtc ties.
            .OrderByDescending(r => r.CreatedAtUtc)
            .ThenByDescending(r => r.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(Guid? requesterId, Guid? departmentId, CancellationToken cancellationToken = default) =>
        Scoped(requesterId, departmentId).CountAsync(cancellationToken);

    // Same scope filters for both the page and its count, so the total can't drift from the list.
    private IQueryable<Request> Scoped(Guid? requesterId, Guid? departmentId)
    {
        var query = dbContext.Requests.AsNoTracking();

        if (requesterId is { } requester)
        {
            query = query.Where(r => r.RequesterId == requester);
        }

        if (departmentId is { } department)
        {
            query = query.Where(r => r.DepartmentId == department);
        }

        return query;
    }
}
