using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// Reports & Analytics (Phase 18): the server-side aggregation endpoints and their authorization.
/// The integration DB is shared across the collection, so assertions are isolation-safe — internal
/// consistency (total == Σ buckets), descending order, and that seeded keys are present with a
/// count of at least the rows this test created — never exact global totals.
/// </summary>
[Collection("Integration")]
public sealed class ReportsTests(ApiFactory factory)
{
    [Fact]
    public async Task RequestReport_AsEmployee_Returns403()
    {
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));

        var response = await employee.GetAsync("/reports/requests");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); // analytics is an oversight view, not for plain Employees
    }

    [Fact]
    public async Task RequestReport_AsAuditor_Returns200()
    {
        var auditor = factory.CreateClientWithToken(await factory.GetTokenAsync("auditor.user"));

        var response = await auditor.GetAsync("/reports/requests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // read-only Auditor may read reports
    }

    [Fact]
    public async Task Reports_AsFinance_Returns200_OnBothEndpoints()
    {
        var finance = factory.CreateClientWithToken(await factory.GetTokenAsync("finance.user"));

        // Finance is an oversight role: it reads both reports (the role analytics matters most for).
        Assert.Equal(HttpStatusCode.OK, (await finance.GetAsync("/reports/requests")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await finance.GetAsync("/reports/assets")).StatusCode);
    }

    [Fact]
    public async Task RequestReport_AggregatesByCategory_WithConsistentTotals()
    {
        // Seed a known request, then read the report as a Manager.
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));
        (await employee.PostAsJsonAsync("/requests", new { title = "Report seed", type = "Procurement" })).EnsureSuccessStatusCode();

        var manager = factory.CreateClientWithToken(await factory.GetTokenAsync("manager.eng"));
        var report = await manager.GetFromJsonAsync<RequestReport>("/reports/requests");

        Assert.NotNull(report);
        Assert.Equal(report!.Total, report.ByStatus.Sum(b => b.Count)); // total is consistent with the buckets
        Assert.Contains(report.ByStatus, b => b.Key == "Draft" && b.Count >= 1); // the seeded request is a Draft
        Assert.Contains(report.ByType, b => b.Key == "Procurement" && b.Count >= 1);
        Assert.Contains(report.ByPriority, b => b.Key == "Medium" && b.Count >= 1); // defaulted priority
        AssertDescendingByCount(report.ByStatus);
    }

    [Fact]
    public async Task AssetReport_AsItAdmin_AggregatesByCategory_WithConsistentTotals()
    {
        var itAdmin = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));
        (await itAdmin.PostAsJsonAsync("/assets", new { tag = "AST-RPT-1", name = "Report seed laptop", type = "Laptop" }))
            .EnsureSuccessStatusCode();

        var report = await itAdmin.GetFromJsonAsync<AssetReport>("/reports/assets");

        Assert.NotNull(report);
        Assert.Equal(report!.Total, report.ByStatus.Sum(b => b.Count));
        Assert.Contains(report.ByStatus, b => b.Key == "InStock" && b.Count >= 1); // newly registered asset
        Assert.Contains(report.ByType, b => b.Key == "Laptop" && b.Count >= 1);
        AssertDescendingByCount(report.ByType);
    }

    private static void AssertDescendingByCount(IReadOnlyList<CategoryCount> buckets)
    {
        for (var i = 1; i < buckets.Count; i++)
        {
            Assert.True(buckets[i - 1].Count >= buckets[i].Count, "buckets must be ordered by count descending");
        }
    }

    private sealed record CategoryCount(string Key, int Count);

    private sealed record RequestReport(
        int Total,
        IReadOnlyList<CategoryCount> ByStatus,
        IReadOnlyList<CategoryCount> ByType,
        IReadOnlyList<CategoryCount> ByPriority);

    private sealed record AssetReport(int Total, IReadOnlyList<CategoryCount> ByStatus, IReadOnlyList<CategoryCount> ByType);
}
