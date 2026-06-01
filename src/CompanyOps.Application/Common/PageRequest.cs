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

    public int Skip => (Page - 1) * PageSize;
    public int Take => PageSize;

    public PageRequest(int? page = null, int? pageSize = null)
    {
        Page = page is > 0 ? page.Value : 1;
        PageSize = pageSize is { } size ? Math.Clamp(size, 1, MaxPageSize) : DefaultPageSize;
    }
}
