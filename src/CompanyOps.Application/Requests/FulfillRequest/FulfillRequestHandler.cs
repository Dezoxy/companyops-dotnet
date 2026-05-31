using CompanyOps.Application.Abstractions;
using CompanyOps.Application.IntegrationEvents;
using CompanyOps.Domain.Auditing;
using CompanyOps.Domain.Common;
using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests.FulfillRequest;

/// <summary>
/// Handles <see cref="FulfillRequestCommand"/>: load the aggregate, call the domain
/// fulfillment (Approved → Completed), persist, return the updated read model. Returns
/// <c>null</c> when the request does not exist.
/// <para>
/// The fulfillment <em>action</em> differs by request type (ADR 0005), and this handler is
/// where that fan-out lives. An <see cref="RequestType.AssetLifecycle"/> request is fulfilled
/// by assigning a concrete in-stock <c>Asset</c> to the requester — a real internal transition
/// committed in the same unit of work. Procurement/Helpdesk keep the out-of-band path: enqueue
/// <see cref="RequestFulfilled"/> so the Worker reserves the asset in the external Inventory
/// system (ADR 0008). The two aggregates (Request, Asset) are coordinated here; each still
/// enforces its own invariants in the Domain.
/// </para>
/// </summary>
public sealed class FulfillRequestHandler(
    IRequestRepository requests,
    IAssetRepository assets,
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

        // The Domain is the single gate for type/asset-presence/status: AssetLifecycle requires
        // an asset id, every other type rejects one. It also records the request → asset link.
        request.Fulfill(command.ActorId, command.AssignedAssetId, now);
        auditLogger.Add(AuditLog.ForRequest(AuditAction.RequestFulfilled, request.Id, command.ActorId, fromStatus, request.Status, now));

        if (request.Type == RequestType.AssetLifecycle)
        {
            // Perform the internal asset transition the fulfillment represents. FulfilledAssetId
            // is non-null here (Fulfill enforced it). Nothing is committed until SaveChanges, so a
            // throw below (asset gone / not in stock) rolls back the whole fulfillment.
            var asset = await assets.GetForUpdateAsync(request.FulfilledAssetId!.Value, cancellationToken)
                ?? throw new DomainException("The asset selected for fulfillment no longer exists.");

            var assetFrom = asset.Status;
            asset.Assign(request.RequesterId, now); // throws if the asset is not in stock
            auditLogger.Add(AuditLog.ForAsset(AuditAction.AssetAssigned, asset.Id, command.ActorId, assetFrom, asset.Status, now));
        }
        else
        {
            // Reserve the asset out-of-band in the external Inventory system: the Worker reacts (ADR 0008).
            eventPublisher.Enqueue(new RequestFulfilled(request.Id, command.ActorId, now));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RequestDto.FromDomain(request);
    }
}
