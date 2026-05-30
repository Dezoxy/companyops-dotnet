using System.Text.Json;
using CompanyOps.Application;
using CompanyOps.Application.Abstractions;
using CompanyOps.Application.ExternalSystems;
using CompanyOps.Application.IntegrationEvents;
using CompanyOps.Domain.Auditing;

namespace CompanyOps.Worker;

/// <summary>
/// Handles one integration message: dedup → call the external system → record the
/// outcome as audit → mark processed, all committed in one transaction. Idempotent
/// (ADR 0008): a redelivered message id is skipped. A gateway failure propagates so the
/// consumer requeues it; an unknown/bad message throws <see cref="MalformedMessageException"/>.
/// </summary>
public sealed class IntegrationEventProcessor(
    IProcessedMessageGuard processedMessages,
    IFinanceGateway financeGateway,
    IInventoryGateway inventoryGateway,
    IAuditLogger auditLogger,
    INotificationSimulator notifier,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<IntegrationEventProcessor> logger)
{
    public async Task ProcessAsync(Guid messageId, string type, string payload, CancellationToken cancellationToken)
    {
        if (await processedMessages.AlreadyProcessedAsync(messageId, cancellationToken))
        {
            logger.LogInformation("Skipping already-processed message {MessageId}.", messageId);
            return;
        }

        var now = timeProvider.GetUtcNow();

        switch (type)
        {
            case nameof(RequestApproved):
                {
                    var approved = Deserialize<RequestApproved>(payload);
                    await financeGateway.CommitBudgetAsync(approved.RequestId, approved.DepartmentId, cancellationToken);
                    await notifier.NotifyApprovedAsync(approved, cancellationToken);
                    auditLogger.Add(AuditLog.ForRequestEvent(AuditAction.BudgetCommitted, approved.RequestId, WellKnownActors.SystemWorker, now));
                    break;
                }

            case nameof(RequestFulfilled):
                {
                    var fulfilled = Deserialize<RequestFulfilled>(payload);
                    await inventoryGateway.ReserveAssetAsync(fulfilled.RequestId, cancellationToken);
                    auditLogger.Add(AuditLog.ForRequestEvent(AuditAction.AssetReserved, fulfilled.RequestId, WellKnownActors.SystemWorker, now));
                    break;
                }

            default:
                throw new MalformedMessageException($"Unknown integration event type '{type}'.");
        }

        processedMessages.MarkProcessed(messageId, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static T Deserialize<T>(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(payload)
                ?? throw new MalformedMessageException("Empty integration-event payload.");
        }
        catch (JsonException ex)
        {
            throw new MalformedMessageException("Invalid integration-event payload.", ex);
        }
    }
}
