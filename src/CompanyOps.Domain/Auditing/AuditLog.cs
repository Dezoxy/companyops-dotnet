using CompanyOps.Domain.Assets;
using CompanyOps.Domain.Common;
using CompanyOps.Domain.Requests;

namespace CompanyOps.Domain.Auditing;

/// <summary>
/// An append-only record of a meaningful state change: who / what / when / old→new /
/// affected object (AGENTS.md non-negotiable). Its content is fixed at creation — the only
/// post-construction write is the writer stamping the source IP once (<see cref="StampSource"/>);
/// there is no path to change a persisted entry. Created via the <see cref="ForRequest"/> factory
/// and persisted through the <c>IAuditLogger</c> port.
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

    /// <summary>
    /// Source IP of the HTTP request that triggered the action; null for actions with no HTTP
    /// origin (e.g. the Worker's external-integration outcomes). Stamped by the audit writer at
    /// persist time via <see cref="StampSource"/> — the Application handlers don't carry it.
    /// </summary>
    public string? SourceIp { get; private set; }

    /// <summary>
    /// The user the action affected beyond the actor — for an asset custody change, the assignee on
    /// assign or the prior holder on reclaim / send-to-repair / retire. Null where not applicable.
    /// Lets the asset history answer "who held it" without replaying the whole trail. Recorded at
    /// creation by the factory (unlike <see cref="SourceIp"/>, which is request-context metadata).
    /// </summary>
    public Guid? AffectedUserId { get; private set; }

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
        string? toStatus,
        Guid? affectedUserId = null)
    {
        Id = id;
        OccurredAtUtc = occurredAtUtc;
        ActorId = actorId;
        Action = action;
        TargetType = targetType;
        TargetId = targetId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        AffectedUserId = affectedUserId;
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

    /// <summary>
    /// Record an action against an <see cref="Asset"/>. <paramref name="fromStatus"/> is the
    /// status before the action (null for registration); <paramref name="toStatus"/> after.
    /// <paramref name="affectedUserId"/> is the holder the action concerns — the assignee on
    /// assign, or the prior holder on reclaim / send-to-repair / retire — null where none.
    /// </summary>
    public static AuditLog ForAsset(
        AuditAction action,
        Guid assetId,
        Guid actorId,
        AssetStatus? fromStatus,
        AssetStatus toStatus,
        DateTimeOffset nowUtc,
        Guid? affectedUserId = null)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException("An audit entry must record the actor.");
        }

        if (assetId == Guid.Empty)
        {
            throw new DomainException("An audit entry must record the affected asset.");
        }

        return new AuditLog(
            Guid.NewGuid(),
            nowUtc,
            actorId,
            action,
            "Asset",
            assetId,
            fromStatus?.ToString(),
            toStatus.ToString(),
            affectedUserId);
    }

    /// <summary>
    /// Stamp the originating request's source IP. Called once by the audit writer at persist time
    /// (the IP is request-context metadata the Application handlers don't carry). Write-once:
    /// ignored if already set, preserving the entry's immutability once recorded.
    /// </summary>
    public void StampSource(string? sourceIp) => SourceIp ??= sourceIp;
}
