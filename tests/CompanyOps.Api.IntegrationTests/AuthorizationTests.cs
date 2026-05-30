using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// End-to-end authorization tests: the real API behind real Keycloak JWTs, asserting
/// the docs/security.md matrix (authentication, RBAC, department IDOR, submit-own) and
/// the happy path. Uses the seed users from the committed realm.
/// </summary>
[Collection("Integration")]
public sealed class AuthorizationTests(ApiFactory factory)
{
    private static readonly Guid EmployeeEngSub = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid EngineeringDept = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/requests");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsEmployee_Returns201_AndDerivesIdentityFromToken()
    {
        var client = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));

        var response = await client.PostAsJsonAsync("/requests", new { title = "New laptop", type = "Procurement" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<RequestResponse>();
        Assert.Equal("Draft", created!.Status);
        Assert.Equal(EmployeeEngSub, created.RequesterId);       // from the JWT sub, not the body
        Assert.Equal(EngineeringDept, created.DepartmentId);     // from the department claim
    }

    [Fact]
    public async Task Create_AsAuditor_Returns403()
    {
        var client = factory.CreateClientWithToken(await factory.GetTokenAsync("auditor.user"));

        var response = await client.PostAsJsonAsync("/requests", new { title = "X", type = "Procurement" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Approve_AsAuditor_Returns403()
    {
        var id = await CreateSubmittedRequestAsync();
        var auditor = factory.CreateClientWithToken(await factory.GetTokenAsync("auditor.user"));

        var response = await auditor.PostAsJsonAsync($"/requests/{id}/approve", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Approve_ByManagerFromAnotherDepartment_Returns400()
    {
        var id = await CreateSubmittedRequestAsync();
        var salesManager = factory.CreateClientWithToken(await factory.GetTokenAsync("manager.sales"));

        var response = await salesManager.PostAsJsonAsync($"/requests/{id}/approve", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); // domain dept-scope (IDOR)
    }

    [Fact]
    public async Task Submit_ByNonRequester_Returns400()
    {
        var id = await CreateDraftRequestAsync();
        // manager.eng holds Employee too, so the policy passes — but they are not the requester.
        var other = factory.CreateClientWithToken(await factory.GetTokenAsync("manager.eng"));

        var response = await other.PostAsync($"/requests/{id}/submit", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FullProcurementFlow_ReachesCompleted()
    {
        var id = await CreateSubmittedRequestAsync();

        var manager = factory.CreateClientWithToken(await factory.GetTokenAsync("manager.eng"));
        var managerStep = await manager.PostAsJsonAsync($"/requests/{id}/approve", new { note = "ok" });
        Assert.Equal(HttpStatusCode.OK, managerStep.StatusCode);

        var finance = factory.CreateClientWithToken(await factory.GetTokenAsync("finance.user"));
        var financeStep = await finance.PostAsJsonAsync($"/requests/{id}/approve", new { });
        Assert.Equal(HttpStatusCode.OK, financeStep.StatusCode);
        Assert.Equal("Approved", (await financeStep.Content.ReadFromJsonAsync<RequestResponse>())!.Status);

        var itAdmin = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));
        var fulfill = await itAdmin.PostAsync($"/requests/{id}/fulfill", content: null);
        Assert.Equal(HttpStatusCode.OK, fulfill.StatusCode);
        Assert.Equal("Completed", (await fulfill.Content.ReadFromJsonAsync<RequestResponse>())!.Status);
    }

    [Fact]
    public async Task GetAuditLogs_AsEmployee_Returns403()
    {
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));

        var response = await employee.GetAsync("/audit-logs");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_AsAuditor_Returns200_AndRecordsTheFlow()
    {
        var id = await CreateSubmittedRequestAsync();
        var manager = factory.CreateClientWithToken(await factory.GetTokenAsync("manager.eng"));
        await manager.PostAsJsonAsync($"/requests/{id}/approve", new { note = "ok" });
        var finance = factory.CreateClientWithToken(await factory.GetTokenAsync("finance.user"));
        await finance.PostAsJsonAsync($"/requests/{id}/approve", new { });
        var itAdmin = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));
        await itAdmin.PostAsync($"/requests/{id}/fulfill", content: null);

        var auditor = factory.CreateClientWithToken(await factory.GetTokenAsync("auditor.user"));
        var response = await auditor.GetAsync("/audit-logs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var entries = (await response.Content.ReadFromJsonAsync<List<AuditLogResponse>>())!
            .Where(e => e.TargetId == id)
            .Select(e => e.Action)
            .ToList();

        // Created, Submitted, two Approved (manager + finance), Fulfilled.
        Assert.Contains("RequestCreated", entries);
        Assert.Contains("RequestSubmitted", entries);
        Assert.Contains("RequestApproved", entries);
        Assert.Contains("RequestFulfilled", entries);
        Assert.Equal(2, entries.Count(a => a == "RequestApproved"));
    }

    private async Task<Guid> CreateDraftRequestAsync()
    {
        var client = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));
        var response = await client.PostAsJsonAsync("/requests", new { title = "New laptop", type = "Procurement" });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<RequestResponse>();
        return created!.Id;
    }

    private async Task<Guid> CreateSubmittedRequestAsync()
    {
        var id = await CreateDraftRequestAsync();
        var client = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));
        var submit = await client.PostAsync($"/requests/{id}/submit", content: null);
        submit.EnsureSuccessStatusCode();
        return id;
    }

    private sealed record RequestResponse(Guid Id, string Status, Guid RequesterId, Guid DepartmentId);

    private sealed record AuditLogResponse(string Action, Guid TargetId, string? FromStatus, string? ToStatus);
}
