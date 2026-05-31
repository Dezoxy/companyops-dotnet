using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace CompanyOps.Infrastructure.Persistence;

internal sealed class CommentRepository(AppDbContext dbContext) : ICommentRepository
{
    public void Add(Comment comment) => dbContext.Comments.Add(comment);

    public async Task<IReadOnlyList<Comment>> ListByRequestAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        await dbContext.Comments
            .AsNoTracking()
            .Where(c => c.RequestId == requestId)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
