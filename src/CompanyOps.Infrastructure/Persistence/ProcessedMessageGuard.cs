using CompanyOps.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CompanyOps.Infrastructure.Persistence;

internal sealed class ProcessedMessageGuard(AppDbContext dbContext) : IProcessedMessageGuard
{
    public Task<bool> AlreadyProcessedAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ProcessedMessage>().AnyAsync(m => m.Id == messageId, cancellationToken);

    public void MarkProcessed(Guid messageId, DateTimeOffset processedAtUtc) =>
        dbContext.Set<ProcessedMessage>().Add(new ProcessedMessage(messageId, processedAtUtc));
}
