using CompanyOps.Application.Abstractions;
using CompanyOps.Application.IntegrationEvents;
using CompanyOps.Domain.Auditing;
using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests.ApproveRequest;

/// <summary>
/// Handles <see cref="ApproveRequestCommand"/>: load the aggregate, call the domain
/// approval (which enforces role + department scope and advances the chain), persist,
/// return the updated read model. Returns <c>null</c> when the request does not exist.
/// </summary>
public sealed class ApproveRequestHandler(
    IRequestRepository requests,
    IAuditLogger auditLogger,
    IIntegrationEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RequestDto?> HandleAsync(ApproveRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetForUpdateAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var fromStatus = request.Status;

        request.Approve(command.ApproverId, command.ApproverRole, command.ApproverDepartmentId, now, command.Note);
        auditLogger.Add(AuditLog.ForRequest(AuditAction.RequestApproved, request.Id, command.ApproverId, fromStatus, request.Status, now));

        // The final required approval moved the request to Approved — emit the event
        // (to the outbox, same transaction) for the Worker to react to. Earlier steps
        // leave it Submitted and emit nothing.
        if (fromStatus != RequestStatus.Approved && request.Status == RequestStatus.Approved)
        {
            eventPublisher.Enqueue(new RequestApproved(request.Id, request.RequesterId, request.DepartmentId, now));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RequestDto.FromDomain(request);
    }
}
