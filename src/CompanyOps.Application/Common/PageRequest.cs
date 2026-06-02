namespace CompanyOps.Application.Common;

/// <summary>
/// Normalized paging window for list queries. Constructed from optional client input (page +
/// pageSize); out-of-range values are clamped rather than rejected — a 1-based <see cref="Page"/>
/// and a <see cref="PageSize"/> bounded to [1, <see cref="MaxPageSize"/>], defaulting to
/// <see cref="DefaultPageSize"/>. Keeps the list endpoints from returning unbounded result sets.
/// </summary>
public sealed record PageRequest
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    public int Page { get; }
    public int PageSize { get; }

    // Compute in long and saturate: a very large Page (e.g. ?page=10737420&pageSize=200) would
    // overflow int and produce a negative offset → a 500 from EF/Postgres. Saturating to int.MaxValue
    // instead returns an empty page, which is the intended "past the end" behaviour.
    public int Skip => (int)Math.Min((long)(Page - 1) * PageSize, int.MaxValue);
    public int Take => PageSize;

    public PageRequest(int? page = null, int? pageSize = null)
    {
        Page = page is > 0 ? page.Value : 1;
        PageSize = pageSize is { } size ? Math.Clamp(size, 1, MaxPageSize) : DefaultPageSize;
    }
}
