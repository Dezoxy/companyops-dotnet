using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Auditing;

namespace CompanyOps.Application.Requests.SubmitRequest;

/// <summary>
/// Handles <see cref="SubmitRequestCommand"/>: load the aggregate, call the domain
/// transition (which materializes the chain and enforces the rule), persist, return
/// the updated read model. Returns <c>null</c> when the request does not exist.
/// </summary>
public sealed class SubmitRequestHandler(
    IRequestRepository requests,
    IAuditLogger auditLogger,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RequestDto?> HandleAsync(SubmitRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetForUpdateAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var fromStatus = request.Status;

        request.Submit(command.ActorId, now);
        auditLogger.Add(AuditLog.ForRequest(AuditAction.RequestSubmitted, request.Id, command.ActorId, fromStatus, request.Status, now));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RequestDto.FromDomain(request);
    }
}
