using CompanyOps.Application.IntegrationEvents;

namespace CompanyOps.Worker;

/// <summary>
/// Simulates sending a notification in response to an approval (Phase 5 "simulate
/// notification sending"). Must be idempotent — delivery is at-least-once (ADR 0007).
/// </summary>
public interface INotificationSimulator
{
    Task NotifyApprovedAsync(RequestApproved approved, CancellationToken cancellationToken);
}

internal sealed class LoggingNotificationSimulator(ILogger<LoggingNotificationSimulator> logger)
    : INotificationSimulator
{
    public Task NotifyApprovedAsync(RequestApproved approved, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Notification (simulated): request {RequestId} for requester {RequesterId} (dept {DepartmentId}) was approved at {ApprovedAtUtc:o}.",
            approved.RequestId, approved.RequesterId, approved.DepartmentId, approved.ApprovedAtUtc);
        return Task.CompletedTask;
    }
}
