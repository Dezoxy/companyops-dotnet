using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// The authorization matrix and invalid workflow transitions, exercised over HTTP
/// (docs/security.md). Role policies are enforced at the boundary (403); illegal
/// transitions are rejected by the Domain and surface as 400.
/// </summary>
[Collection("Integration")]
public sealed class WorkflowAuthorizationTests(ApiFactory factory)
{
    // --- Role policy (403) ----------------------------------------------------

    [Fact]
    public async Task Approve_AsEmployee_Returns403()
    {
        var id = await SubmittedRequestAsync();
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));

        var response = await employee.PostAsJsonAsync($"/requests/{id}/approve", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Approve_AsItAdmin_Returns403()
    {
        var id = await SubmittedRequestAsync();
        var itAdmin = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));

        var response = await itAdmin.PostAsJsonAsync($"/requests/{id}/approve", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Fulfill_AsManager_Returns403()
    {
        var id = await SubmittedRequestAsync();
        var manager = factory.CreateClientWithToken(await factory.GetTokenAsync("manager.eng"));

        var response = await manager.PostAsync($"/requests/{id}/fulfill", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- Invalid transitions (400) -------------------------------------------

    [Fact]
    public async Task Approve_BeforeSubmit_Returns400()
    {
        var id = await DraftRequestAsync(); // still Draft, not Submitted
        var manager = factory.CreateClientWithToken(await factory.GetTokenAsync("manager.eng"));

        var response = await manager.PostAsJsonAsync($"/requests/{id}/approve", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Fulfill_BeforeApproved_Returns400()
    {
        var id = await SubmittedRequestAsync(); // Submitted, not Approved
        var itAdmin = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));

        var response = await itAdmin.PostAsync($"/requests/{id}/fulfill", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_Twice_Returns400()
    {
        var id = await SubmittedRequestAsync();
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));

        var response = await employee.PostAsync($"/requests/{id}/submit", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Reject flow ----------------------------------------------------------

    [Fact]
    public async Task Reject_ByManager_TransitionsToRejected()
    {
        var id = await SubmittedRequestAsync();
        var manager = factory.CreateClientWithToken(await factory.GetTokenAsync("manager.eng"));

        var response = await manager.PostAsJsonAsync($"/requests/{id}/reject", new { reason = "Out of budget" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<StatusResponse>();
        Assert.Equal("Rejected", dto!.Status);
    }

    private async Task<Guid> DraftRequestAsync()
    {
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));
        var created = await employee.PostAsJsonAsync("/requests", new { title = "Laptop", type = "Procurement" });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<StatusResponse>())!.Id;
    }

    private async Task<Guid> SubmittedRequestAsync()
    {
        var id = await DraftRequestAsync();
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));
        (await employee.PostAsync($"/requests/{id}/submit", content: null)).EnsureSuccessStatusCode();
        return id;
    }

    private sealed record StatusResponse(Guid Id, string Status);
}
