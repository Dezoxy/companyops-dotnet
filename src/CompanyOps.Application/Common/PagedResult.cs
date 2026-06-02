using System.Text.Json.Serialization;

namespace CompanyOps.Application.Common;

/// <summary>
/// A single page of a list query plus the total across all pages, so clients can render a
/// pagination footer ("Showing 1–50 of 142") and page controls. <see cref="Items"/> is the page;
/// <see cref="Total"/> is the unpaged count; <see cref="Page"/>/<see cref="PageSize"/> echo the
/// normalized window that produced it.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    /// <summary>
    /// Number of pages at the current <see cref="PageSize"/> (at least 1). A server-side helper —
    /// kept off the wire (clients derive it from Total/PageSize) so the contract has no redundant,
    /// potentially-divergent field.
    /// </summary>
    [JsonIgnore]
    public int TotalPages => Total <= 0 ? 1 : (Total + PageSize - 1) / PageSize;
}
