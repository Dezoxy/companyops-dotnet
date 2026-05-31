using CompanyOps.Application.Abstractions;

namespace CompanyOps.Application.Reports;

/// <summary>
/// Read use-cases for the Reports & Analytics screen (Phase 18). Thin: they delegate to the
/// reporting read port, which aggregates server-side. The handler is the seam where a future
/// date-range filter or department scope would attach.
/// </summary>
public sealed record GetRequestReportQuery;

public sealed class GetRequestReportHandler(IReportingStore reports)
{
    public Task<RequestReportDto> HandleAsync(GetRequestReportQuery query, CancellationToken cancellationToken = default) =>
        reports.GetRequestReportAsync(cancellationToken);
}

public sealed record GetAssetReportQuery;

public sealed class GetAssetReportHandler(IReportingStore reports)
{
    public Task<AssetReportDto> HandleAsync(GetAssetReportQuery query, CancellationToken cancellationToken = default) =>
        reports.GetAssetReportAsync(cancellationToken);
}
