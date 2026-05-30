using CompanyOps.Domain.Auditing;

namespace CompanyOps.Application.Abstractions;

/// <summary>
/// Port for recording audit entries. Symmetric with <see cref="IRequestRepository.Add"/>:
/// the handler enlists the entry, and the existing <see cref="IUnitOfWork.SaveChangesAsync"/>
/// commits it in the **same transaction** as the state change — so there is never an
/// approved-but-unaudited request. The concrete writer lives in Infrastructure.
/// </summary>
public interface IAuditLogger
{
    void Add(AuditLog entry);
}
