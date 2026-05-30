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

    public async Task<IReadOnlyList<Request>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Requests
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
