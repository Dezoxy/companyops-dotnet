namespace CompanyOps.Application.Abstractions;

/// <summary>
/// Commits all changes tracked within the current unit of work as one transaction.
/// Kept separate from the repository so a handler can compose several repository
/// operations into a single atomic save.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
