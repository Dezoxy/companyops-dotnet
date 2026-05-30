using CompanyOps.Domain.Auditing;

namespace CompanyOps.Application.Auditing;

/// <summary>Read model for an audit entry returned across the Application boundary.</summary>
public sealed record AuditLogDto(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    Guid ActorId,
    AuditAction Action,
    string TargetType,
    Guid TargetId,
    string? FromStatus,
    string? ToStatus)
{
    public static AuditLogDto FromDomain(AuditLog log) => new(
        log.Id,
        log.OccurredAtUtc,
        log.ActorId,
        log.Action,
        log.TargetType,
        log.TargetId,
        log.FromStatus,
        log.ToStatus);
}
