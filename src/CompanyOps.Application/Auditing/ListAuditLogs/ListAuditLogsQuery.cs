namespace CompanyOps.Application.Auditing.ListAuditLogs;

/// <summary>
/// Use-case input for reading the audit trail. No filters yet (Phase 4); the reader
/// caps rows. Follow-up: add target/actor/date filters + limit/offset (or cursor) paging.
/// </summary>
public sealed record ListAuditLogsQuery;
