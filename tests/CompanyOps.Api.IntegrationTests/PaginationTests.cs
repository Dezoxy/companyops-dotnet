using System.Net.Http.Json;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// List endpoints page their results (page + pageSize query params) instead of returning the whole
/// table — see Application.Common.PageRequest. Uses the asset console (IT Admin can both seed and
/// list), which exercises the same PageRequest path as /requests and /audit-logs.
/// </summary>
[Collection("Integration")]
public sealed class PaginationTests(ApiFactory factory)
{
    private sealed record AssetResponse(Guid Id);

    private sealed record PagedResponse<T>(List<T> Items, int Total, int Page, int PageSize);

    [Fact]
    public async Task ListAssets_PagesResults_WithDisjointPages()
    {
        var client = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));

        // Seed enough assets to span two pages of size 2.
        var prefix = $"PAGE-{Guid.NewGuid():N}";
        for (var i = 0; i < 3; i++)
        {
            var r = await client.PostAsJsonAsync("/assets", new { tag = $"{prefix}-{i}", name = "Pager", type = "Laptop" });
            r.EnsureSuccessStatusCode();
        }

        var page1 = await client.GetFromJsonAsync<PagedResponse<AssetResponse>>("/assets?page=1&pageSize=2");
        var page2 = await client.GetFromJsonAsync<PagedResponse<AssetResponse>>("/assets?page=2&pageSize=2");

        Assert.NotNull(page1);
        Assert.NotNull(page2);
        // pageSize is respected: a page never exceeds the requested size.
        Assert.Equal(2, page1!.Items.Count);
        Assert.True(page2!.Items.Count >= 1);
        // The total counts the whole table (≥ the 3 we just seeded), not just the page.
        Assert.True(page1.Total >= 3);
        Assert.Equal(page1.Total, page2.Total);
        // Pages are disjoint — no row appears on both.
        Assert.Empty(page1.Items.Select(a => a.Id).Intersect(page2.Items.Select(a => a.Id)));
    }

    [Fact]
    public async Task ListAssets_OversizedPageSize_IsClampedNotRejected()
    {
        var client = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));

        // Way over MaxPageSize (200) — clamped, not a 400.
        var response = await client.GetAsync("/assets?pageSize=100000");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<AssetResponse>>();
        Assert.NotNull(page);
        Assert.True(page!.Items.Count <= 200);
    }
}
