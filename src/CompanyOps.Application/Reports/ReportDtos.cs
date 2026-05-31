namespace CompanyOps.Application.Reports;

/// <summary>
/// One bucket of an aggregate: the category's key (the Domain enum value's name, e.g.
/// <c>"Approved"</c>) and how many rows fall in it. The frontend maps the key to a label and a
/// status tone using the metadata it already has — the report stays presentation-agnostic.
/// </summary>
public sealed record CategoryCount(string Key, int Count);

/// <summary>
/// Aggregate counts over all requests, grouped server-side (GROUP BY). Buckets are ordered by
/// count descending; only non-empty buckets appear.
/// </summary>
public sealed record RequestReportDto(
    int Total,
    IReadOnlyList<CategoryCount> ByStatus,
    IReadOnlyList<CategoryCount> ByType,
    IReadOnlyList<CategoryCount> ByPriority);

/// <summary>Aggregate counts over all assets, grouped server-side (GROUP BY).</summary>
public sealed record AssetReportDto(
    int Total,
    IReadOnlyList<CategoryCount> ByStatus,
    IReadOnlyList<CategoryCount> ByType);
