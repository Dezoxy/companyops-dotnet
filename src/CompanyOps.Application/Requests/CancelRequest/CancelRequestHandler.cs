using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Auditing;

namespace CompanyOps.Application.Requests.CancelRequest;

/// <summary>
/// Handles <see cref="CancelRequestCommand"/>: load the aggregate, call the domain cancel (which
/// enforces requester-or-department-manager + Draft/Submitted), persist, return the updated read
/// model. Returns <c>null</c> when the request does not exist.
/// </summary>
public sealed class CancelRequestHandler(
    IRequestRepository requests,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RequestDto?> HandleAsync(CancelRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetForUpdateAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var fromStatus = request.Status;

        request.Cancel(command.ActorId, command.ActorRoles, command.ActorDepartmentId, now);
        auditLogger.Add(AuditLog.ForRequest(AuditAction.RequestCancelled, request.Id, command.ActorId, fromStatus, request.Status, now));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RequestDto.FromDomain(request);
    }
}
