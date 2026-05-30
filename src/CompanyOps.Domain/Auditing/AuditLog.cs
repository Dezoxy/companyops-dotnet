using CompanyOps.Domain.Common;
using CompanyOps.Domain.Requests;

namespace CompanyOps.Domain.Auditing;

/// <summary>
/// An append-only record of a meaningful state change: who / what / when / old→new /
/// affected object (AGENTS.md non-negotiable). There is no mutator and no factory that
/// changes an existing entry — once written, an audit record is immutable. Created via
/// the <see cref="ForRequest"/> factory and persisted through the <c>IAuditLogger</c> port.
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public Guid ActorId { get; private set; }
    public AuditAction Action { get; private set; }

    /// <summary>The kind of object affected (e.g. "Request") — lets the log span aggregates later.</summary>
    public string TargetType { get; private set; } = null!;
    public Guid TargetId { get; private set; }

    /// <summary>Old → new status of the target, as readable names. Null where not applicable (e.g. creation).</summary>
    public string? FromStatus { get; private set; }
    public string? ToStatus { get; private set; }

    // Required by EF Core's materializer; not for application use.
    private AuditLog()
    {
    }

    private AuditLog(
        Guid id,
        DateTimeOffset occurredAtUtc,
        Guid actorId,
        AuditAction action,
        string targetType,
        Guid targetId,
        string? fromStatus,
        string? toStatus)
    {
        Id = id;
        OccurredAtUtc = occurredAtUtc;
        ActorId = actorId;
        Action = action;
        TargetType = targetType;
        TargetId = targetId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
    }

    /// <summary>
    /// Record an action against a <see cref="Request"/>. <paramref name="fromStatus"/> is
    /// the status before the action (null for creation); <paramref name="toStatus"/> after.
    /// </summary>
    public static AuditLog ForRequest(
        AuditAction action,
        Guid requestId,
        Guid actorId,
        RequestStatus? fromStatus,
        RequestStatus toStatus,
        DateTimeOffset nowUtc)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException("An audit entry must record the actor.");
        }

        if (requestId == Guid.Empty)
        {
            throw new DomainException("An audit entry must record the affected request.");
        }

        return new AuditLog(
            Guid.NewGuid(),
            nowUtc,
            actorId,
            action,
            "Request",
            requestId,
            fromStatus?.ToString(),
            toStatus.ToString());
    }

    /// <summary>
    /// Record an action against a request that is not a status transition (e.g. an
    /// external-integration outcome like budget committed / asset reserved).
    /// </summary>
    public static AuditLog ForRequestEvent(AuditAction action, Guid requestId, Guid actorId, DateTimeOffset nowUtc)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException("An audit entry must record the actor.");
        }

        if (requestId == Guid.Empty)
        {
            throw new DomainException("An audit entry must record the affected request.");
        }

        return new AuditLog(Guid.NewGuid(), nowUtc, actorId, action, "Request", requestId, null, null);
    }
}
