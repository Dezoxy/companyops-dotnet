using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// The request-driven asset-lifecycle flow (Phase 16c): an employee raises an
/// <c>AssetLifecycle</c> request, the manager approves (manager-only chain), then IT fulfils by
/// assigning a concrete in-stock asset to the requester. Fulfillment performs the real internal
/// <c>Asset.Assign</c> transition in the same transaction and links the request to the asset.
/// Exercised end-to-end through real Keycloak JWTs against real Postgres.
/// </summary>
[Collection("Integration")]
public sealed class AssetLifecycleFlowTests(ApiFactory factory)
{
    private static readonly Guid EmployeeEngSub = Guid.Parse("10000000-0000-0000-0000-000000000001");

    private async Task<Guid> ApprovedAssetRequestAsync(string title)
    {
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));
        var created = await employee.PostAsJsonAsync("/requests", new { title, type = "AssetLifecycle" });
        created.EnsureSuccessStatusCode();
        var id = (await created.Content.ReadFromJsonAsync<RequestResponse>())!.Id;

        (await employee.PostAsync($"/requests/{id}/submit", content: null)).EnsureSuccessStatusCode();

        var manager = factory.CreateClientWithToken(await factory.GetTokenAsync("manager.eng"));
        var approved = await manager.PostAsJsonAsync($"/requests/{id}/approve", new { note = "ok" });
        Assert.Equal("Approved", (await approved.Content.ReadFromJsonAsync<RequestResponse>())!.Status); // manager-only chain
        return id;
    }

    private async Task<Guid> RegisterInStockAssetAsync(string tag)
    {
        var itAdmin = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));
        var created = await itAdmin.PostAsJsonAsync("/assets", new { tag, name = "MacBook Pro", type = "Laptop" });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<AssetResponse>())!.Id;
    }

    [Fact]
    public async Task FullFlow_ItAssignsInStockAsset_RequestCompletesAndAssetIsHeldByRequester()
    {
        var requestId = await ApprovedAssetRequestAsync("Need a laptop");
        var assetId = await RegisterInStockAssetAsync("AST-RDL-1");
        var itAdmin = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));

        var fulfill = await itAdmin.PostAsJsonAsync($"/requests/{requestId}/fulfill", new { assignedAssetId = assetId });

        Assert.Equal(HttpStatusCode.OK, fulfill.StatusCode);
        var completed = (await fulfill.Content.ReadFromJsonAsync<RequestResponse>())!;
        Assert.Equal("Completed", completed.Status);
        Assert.Equal(assetId, completed.FulfilledAssetId); // request → asset link recorded

        // The real asset transitioned and is now held by the requester (not the fulfilling IT admin).
        var asset = (await itAdmin.GetFromJsonAsync<AssetResponse>($"/assets/{assetId}"))!;
        Assert.Equal("Assigned", asset.Status);
        Assert.Equal(EmployeeEngSub, asset.AssignedToId);

        // The assignment shows in the asset's own audit-backed history.
        var history = (await itAdmin.GetFromJsonAsync<List<AssetHistoryResponse>>($"/assets/{assetId}/history"))!
            .Select(e => e.Action);
        Assert.Contains("AssetAssigned", history);
    }

    [Fact]
    public async Task Fulfill_WithoutNamingAnAsset_Returns400()
    {
        var requestId = await ApprovedAssetRequestAsync("Need a phone");
        var itAdmin = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));

        var fulfill = await itAdmin.PostAsync($"/requests/{requestId}/fulfill", content: null); // no asset id

        Assert.Equal(HttpStatusCode.BadRequest, fulfill.StatusCode); // domain: asset-lifecycle fulfillment must name an asset
    }

    [Fact]
    public async Task Fulfill_WithAssetNotInStock_Returns400_AndDoesNotCompleteRequest()
    {
        var requestId = await ApprovedAssetRequestAsync("Need a monitor");
        var assetId = await RegisterInStockAssetAsync("AST-RDL-2");
        var itAdmin = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));

        // Take the asset out of stock first (assign it to someone else directly via the console).
        (await itAdmin.PostAsJsonAsync($"/assets/{assetId}/assign", new { userId = Guid.NewGuid() })).EnsureSuccessStatusCode();

        var fulfill = await itAdmin.PostAsJsonAsync($"/requests/{requestId}/fulfill", new { assignedAssetId = assetId });
        Assert.Equal(HttpStatusCode.BadRequest, fulfill.StatusCode); // Asset.Assign rejects a non-in-stock asset

        // The whole fulfillment rolled back — the request is still Approved, not Completed.
        var request = (await itAdmin.GetFromJsonAsync<RequestResponse>($"/requests/{requestId}"))!;
        Assert.Equal("Approved", request.Status);
        Assert.Null(request.FulfilledAssetId);
    }

    private sealed record RequestResponse(Guid Id, string Status, Guid RequesterId, Guid? FulfilledAssetId);

    private sealed record AssetResponse(Guid Id, string Status, Guid? AssignedToId);

    private sealed record AssetHistoryResponse(string Action);
}
