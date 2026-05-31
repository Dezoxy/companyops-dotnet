using CompanyOps.Application.Reports;

namespace CompanyOps.Application.Abstractions;

/// <summary>
/// Read port for reporting aggregates (Phase 18). The implementation aggregates <em>in the
/// database</em> (GROUP BY / COUNT) and returns only the small grouped result — deliberately
/// different from the dashboard, which pulls the rows and counts client-side. Read-only.
/// </summary>
public interface IReportingStore
{
    Task<RequestReportDto> GetRequestReportAsync(CancellationToken cancellationToken = default);

    Task<AssetReportDto> GetAssetReportAsync(CancellationToken cancellationToken = default);
}
