using System.Net.Http.Json;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// Read-scoping on <c>GET /requests</c> (the list): an Employee sees only their own, a Manager
/// their department, and Finance / IT Admin / Auditor see all — mirroring who can act on what.
/// Isolation-safe against the shared DB: assertions check the scope predicate (every returned row
/// matches) plus the cross-scope inclusion/exclusion of requests this test creates, never global
/// counts.
/// </summary>
[Collection("Integration")]
public sealed class RequestReadScopingTests(ApiFactory factory)
{
    private static readonly Guid EmployeeEngSub = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid EngineeringDept = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private async Task<Guid> CreateAsync(string user, string title)
    {
        var client = factory.CreateClientWithToken(await factory.GetTokenAsync(user));
        var created = await client.PostAsJsonAsync("/requests", new { title, type = "Procurement" });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<RequestResponse>())!.Id;
    }

    private async Task<List<RequestResponse>> ListAsync(string user)
    {
        var client = factory.CreateClientWithToken(await factory.GetTokenAsync(user));
        return (await client.GetFromJsonAsync<List<RequestResponse>>("/requests"))!;
    }

    [Fact]
    public async Task Employee_SeesOnlyTheirOwnRequests()
    {
        var mine = await CreateAsync("employee.eng", "scoping: employee own");
        await CreateAsync("manager.eng", "scoping: someone else's"); // a different requester

        var list = await ListAsync("employee.eng");

        Assert.Contains(list, r => r.Id == mine);
        Assert.All(list, r => Assert.Equal(EmployeeEngSub, r.RequesterId)); // never anyone else's
    }

    [Fact]
    public async Task Manager_SeesOnlyTheirDepartment()
    {
        var engineering = await CreateAsync("employee.eng", "scoping: eng request"); // Engineering dept
        var sales = await CreateAsync("manager.sales", "scoping: sales request");     // Sales dept

        var list = await ListAsync("manager.eng");

        Assert.Contains(list, r => r.Id == engineering);
        Assert.DoesNotContain(list, r => r.Id == sales);                       // another department excluded
        Assert.All(list, r => Assert.Equal(EngineeringDept, r.DepartmentId));
    }

    [Fact]
    public async Task Finance_SeesRequestsAcrossDepartments()
    {
        var engineering = await CreateAsync("employee.eng", "scoping: eng for finance");
        var sales = await CreateAsync("manager.sales", "scoping: sales for finance");

        var list = await ListAsync("finance.user");

        Assert.Contains(list, r => r.Id == engineering);
        Assert.Contains(list, r => r.Id == sales); // global view — not department-scoped
    }

    private sealed record RequestResponse(Guid Id, Guid RequesterId, Guid DepartmentId);
}
