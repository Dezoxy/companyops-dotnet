using CompanyOps.Application.Abstractions;
using CompanyOps.Application.IntegrationEvents;
using CompanyOps.Domain.Auditing;

namespace CompanyOps.Application.Requests.FulfillRequest;

/// <summary>
/// Handles <see cref="FulfillRequestCommand"/>: load the aggregate, call the domain
/// fulfillment (Approved → Completed), persist, return the updated read model. Returns
/// <c>null</c> when the request does not exist.
/// </summary>
public sealed class FulfillRequestHandler(
    IRequestRepository requests,
    IAuditLogger auditLogger,
    IIntegrationEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RequestDto?> HandleAsync(FulfillRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetForUpdateAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var fromStatus = request.Status;

        request.Fulfill(command.ActorId, now);
        auditLogger.Add(AuditLog.ForRequest(AuditAction.RequestFulfilled, request.Id, command.ActorId, fromStatus, request.Status, now));

        // Reserve the asset out-of-band: the Worker reacts to this event (ADR 0008).
        eventPublisher.Enqueue(new RequestFulfilled(request.Id, command.ActorId, now));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RequestDto.FromDomain(request);
    }
}
