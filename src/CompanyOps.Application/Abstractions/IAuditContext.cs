namespace CompanyOps.Application.Abstractions;

/// <summary>
/// Ambient provenance for an audited action — the source IP of the originating request. Implemented
/// per host: the API derives it from the current HTTP request; the Worker has none (its audits
/// carry no IP). Keeps the Infrastructure audit writer free of any HTTP dependency.
/// </summary>
public interface IAuditContext
{
    string? SourceIp { get; }
}
