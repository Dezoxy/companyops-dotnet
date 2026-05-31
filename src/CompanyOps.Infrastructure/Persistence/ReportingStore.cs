using System.Linq.Expressions;
using CompanyOps.Application.Abstractions;
using CompanyOps.Application.Reports;
using Microsoft.EntityFrameworkCore;

namespace CompanyOps.Infrastructure.Persistence;

/// <summary>
/// Reporting aggregates computed in the database with <c>GROUP BY</c> + <c>COUNT</c> (Phase 18).
/// The row data never leaves Postgres — only the handful of grouped buckets do — which is the
/// distinction from the dashboard's client-side counting. Read-only (<see cref="EntityFrameworkQueryableExtensions.AsNoTracking{T}"/>).
/// <para>
/// Each report runs one small grouped query per category (so <see cref="RequestReportDto.Total"/>
/// is the sum of the status buckets, not a separate <c>COUNT</c>); under concurrent writes the
/// per-category totals can differ by the odd row. Both are accepted analytics trade-offs —
/// collapsing into a single round-trip / snapshot read is an enterprise-optional follow-up.
/// </para>
/// </summary>
internal sealed class ReportingStore(AppDbContext dbContext) : IReportingStore
{
    public async Task<RequestReportDto> GetRequestReportAsync(CancellationToken cancellationToken = default)
    {
        var byStatus = await CountByAsync(dbContext.Requests, r => r.Status, cancellationToken);
        var byType = await CountByAsync(dbContext.Requests, r => r.Type, cancellationToken);
        var byPriority = await CountByAsync(dbContext.Requests, r => r.Priority, cancellationToken);
        return new RequestReportDto(byStatus.Sum(c => c.Count), byStatus, byType, byPriority);
    }

    public async Task<AssetReportDto> GetAssetReportAsync(CancellationToken cancellationToken = default)
    {
        var byStatus = await CountByAsync(dbContext.Assets, a => a.Status, cancellationToken);
        var byType = await CountByAsync(dbContext.Assets, a => a.Type, cancellationToken);
        return new AssetReportDto(byStatus.Sum(c => c.Count), byStatus, byType);
    }

    /// <summary>
    /// <c>SELECT key, COUNT(*) ... GROUP BY key</c> for an enum column. Only the per-bucket rows
    /// materialize; the enum key is stringified in memory on that tiny set (EF can't translate
    /// <c>Enum.ToString()</c> to SQL). Ordered by count descending for a stable, useful response.
    /// </summary>
    private static async Task<IReadOnlyList<CategoryCount>> CountByAsync<TSource, TKey>(
        IQueryable<TSource> source,
        Expression<Func<TSource, TKey>> keySelector,
        CancellationToken cancellationToken)
        where TSource : class
        where TKey : struct, Enum
    {
        var grouped = await source
            .AsNoTracking()
            .GroupBy(keySelector)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return grouped
            .OrderByDescending(x => x.Count)
            .Select(x => new CategoryCount(x.Key.ToString(), x.Count))
            .ToList();
    }
}
