namespace CompanyOps.Application.IntegrationEvents;

/// <summary>
/// Published when a request reaches <c>Approved</c> (all required steps satisfied).
/// The Worker reacts to it (Phase 5: simulate a notification to the requester).
/// </summary>
public sealed record RequestApproved(
    Guid RequestId,
    Guid RequesterId,
    Guid DepartmentId,
    DateTimeOffset ApprovedAtUtc) : IIntegrationEvent;
