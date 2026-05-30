using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Auditing;
using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests.CreateRequest;

/// <summary>
/// Handles <see cref="CreateRequestCommand"/>: build the aggregate (Domain enforces
/// its invariants), persist it, and return the read model.
/// </summary>
/// <remarks>
/// Plain injectable handler — no mediator yet. A mediator (with validation/logging/
/// audit pipeline behaviours) is introduced later, when those cross-cutting concerns
/// earn the dependency. <see cref="TimeProvider"/> is injected so "now" is testable.
/// </remarks>
public sealed class CreateRequestHandler(
    IRequestRepository requests,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RequestDto> HandleAsync(CreateRequestCommand command, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        var request = Request.Create(
            command.Title,
            command.Description,
            command.Type,
            command.RequesterId,
            command.DepartmentId,
            now);

        requests.Add(request);
        auditLogger.Add(AuditLog.ForRequest(AuditAction.RequestCreated, request.Id, command.RequesterId, null, request.Status, now));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RequestDto.FromDomain(request);
    }
}
