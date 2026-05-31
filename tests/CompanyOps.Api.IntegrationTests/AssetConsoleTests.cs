using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// Asset console end-to-end: the IT-Admin-only lifecycle through real Keycloak JWTs, the
/// state transitions, the authorization gate (others get 403), and the audit-backed history.
/// </summary>
[Collection("Integration")]
public sealed class AssetConsoleTests(ApiFactory factory)
{
    private async Task<HttpClient> ItAdminAsync() =>
        factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));

    private async Task<Guid> RegisterAssetAsync(string tag)
    {
        var client = await ItAdminAsync();
        var response = await client.PostAsJsonAsync("/assets", new { tag, name = "Test laptop", type = "Laptop" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AssetResponse>())!.Id;
    }

    [Fact]
    public async Task List_AsEmployee_Returns403()
    {
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));

        var response = await employee.GetAsync("/assets");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); // not in ReadAssets (IT Admin / Auditor)
    }

    [Fact]
    public async Task List_AsAuditor_Returns200()
    {
        var auditor = factory.CreateClientWithToken(await factory.GetTokenAsync("auditor.user"));

        var response = await auditor.GetAsync("/assets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // Auditor reads everything (ReadAssets)
    }

    [Fact]
    public async Task Register_AsItAdmin_Returns201_InStock()
    {
        var client = await ItAdminAsync();

        var response = await client.PostAsJsonAsync("/assets", new { tag = "AST-REG-1", name = "Dell XPS", type = "Laptop" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var asset = await response.Content.ReadFromJsonAsync<AssetResponse>();
        Assert.Equal("InStock", asset!.Status);
        Assert.Null(asset.AssignedToId);
    }

    [Fact]
    public async Task Assign_AsManager_Returns403()
    {
        var id = await RegisterAssetAsync("AST-AUTHZ-1");
        var manager = factory.CreateClientWithToken(await factory.GetTokenAsync("manager.eng"));

        var response = await manager.PostAsJsonAsync($"/assets/{id}/assign", new { userId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task FullLifecycle_RegisterToRetire_TransitionsThroughEachState()
    {
        var client = await ItAdminAsync();
        var id = await RegisterAssetAsync("AST-LIFE-1");

        async Task<string> Act(string verb) =>
            (await (await client.PostAsync($"/assets/{id}/{verb}", content: null)).Content.ReadFromJsonAsync<AssetResponse>())!.Status;

        var assigned = await client.PostAsJsonAsync($"/assets/{id}/assign", new { userId = Guid.NewGuid() });
        Assert.Equal("Assigned", (await assigned.Content.ReadFromJsonAsync<AssetResponse>())!.Status);
        Assert.Equal("InStock", await Act("reclaim"));
        Assert.Equal("InRepair", await Act("repair"));
        Assert.Equal("InStock", await Act("return-from-repair"));
        Assert.Equal("Retired", await Act("retire"));
    }

    [Fact]
    public async Task GetHistory_RecordsRegisterAndAssign()
    {
        var client = await ItAdminAsync();
        var id = await RegisterAssetAsync("AST-HIST-1");
        await client.PostAsJsonAsync($"/assets/{id}/assign", new { userId = Guid.NewGuid() });

        var actions = (await client.GetFromJsonAsync<List<AssetHistoryResponse>>($"/assets/{id}/history"))!
            .Select(e => e.Action)
            .ToList();

        Assert.Contains("AssetRegistered", actions);
        Assert.Contains("AssetAssigned", actions);
    }

    [Fact]
    public async Task GetById_MissingAsset_Returns404()
    {
        var client = await ItAdminAsync();

        var response = await client.GetAsync($"/assets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record AssetResponse(
        Guid Id,
        string Tag,
        string Name,
        string Type,
        string Status,
        Guid? AssignedToId,
        DateTimeOffset CreatedAtUtc);

    private sealed record AssetHistoryResponse(string Action, string? FromStatus, string? ToStatus);
}
