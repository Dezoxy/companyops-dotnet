using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// Read-scoping on the request reads — both <c>GET /requests</c> (the list) and
/// <c>GET /requests/{id}</c> (the single read), which share the same scope: an Employee sees only
/// their own, a Manager their department, and Finance / IT Admin / Auditor see all — mirroring who
/// can act on what. An out-of-scope single read returns 404, not 403, so a request's existence
/// isn't revealed. Isolation-safe against the shared DB: assertions check the scope predicate
/// (every returned row matches) plus the cross-scope inclusion/exclusion of requests this test
/// creates, never global counts.
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

    private async Task<HttpResponseMessage> GetByIdAsync(string user, Guid id)
    {
        var client = factory.CreateClientWithToken(await factory.GetTokenAsync(user));
        return await client.GetAsync($"/requests/{id}");
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

    [Fact]
    public async Task Employee_GettingAnotherUsersRequestById_GetsNotFound()
    {
        var someoneElses = await CreateAsync("manager.eng", "scoping: by-id not employee's");

        var response = await GetByIdAsync("employee.eng", someoneElses);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); // out of scope reads as absent, not 403
    }

    [Fact]
    public async Task Employee_GettingTheirOwnRequestById_Succeeds()
    {
        var mine = await CreateAsync("employee.eng", "scoping: by-id employee's own");

        var response = await GetByIdAsync("employee.eng", mine);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Manager_GettingAnotherDepartmentsRequestById_GetsNotFound()
    {
        var sales = await CreateAsync("manager.sales", "scoping: by-id sales"); // Sales dept

        var response = await GetByIdAsync("manager.eng", sales); // Engineering manager

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); // another department is out of scope
    }

    [Fact]
    public async Task Manager_GettingTheirDepartmentsRequestById_Succeeds()
    {
        var engineering = await CreateAsync("employee.eng", "scoping: by-id eng"); // same dept as manager.eng

        var response = await GetByIdAsync("manager.eng", engineering);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // own department, even if not their own request
    }

    [Fact]
    public async Task Finance_GettingAnyRequestById_Succeeds()
    {
        var sales = await CreateAsync("manager.sales", "scoping: by-id sales for finance");

        var response = await GetByIdAsync("finance.user", sales);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // global read scope
    }

    [Fact]
    public async Task ItAdmin_GettingAnyRequestById_Succeeds()
    {
        var sales = await CreateAsync("manager.sales", "scoping: by-id sales for itadmin");

        var response = await GetByIdAsync("itadmin.user", sales);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // IT fulfils any department
    }

    [Fact]
    public async Task Auditor_GettingAnyRequestById_Succeeds()
    {
        var sales = await CreateAsync("manager.sales", "scoping: by-id sales for auditor");

        var response = await GetByIdAsync("auditor.user", sales);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // read-only oversight spans departments
    }

    private sealed record RequestResponse(Guid Id, Guid RequesterId, Guid DepartmentId);
}
