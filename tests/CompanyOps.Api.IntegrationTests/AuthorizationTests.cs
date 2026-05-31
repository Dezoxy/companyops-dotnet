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
        Assert.Equal("Medium", created.Priority);                // defaulted when the client omits it
        Assert.Null(created.Category);                           // category is helpdesk-only
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
    public async Task FullHelpdeskFlow_ManagerOnly_ReachesCompleted()
    {
        // Helpdesk runs on the same engine with a different chain: manager-only (no Finance
        // step), so a single manager approval reaches Approved, then IT fulfils.
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));
        var created = await employee.PostAsJsonAsync("/requests", new { title = "VPN access", type = "Helpdesk" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<RequestResponse>())!.Id;

        (await employee.PostAsync($"/requests/{id}/submit", content: null)).EnsureSuccessStatusCode();

        var manager = factory.CreateClientWithToken(await factory.GetTokenAsync("manager.eng"));
        var managerStep = await manager.PostAsJsonAsync($"/requests/{id}/approve", new { note = "approved" });
        Assert.Equal(HttpStatusCode.OK, managerStep.StatusCode);
        // One step is enough — no Finance approval required for helpdesk.
        Assert.Equal("Approved", (await managerStep.Content.ReadFromJsonAsync<RequestResponse>())!.Status);

        var itAdmin = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));
        var fulfill = await itAdmin.PostAsync($"/requests/{id}/fulfill", content: null);
        Assert.Equal(HttpStatusCode.OK, fulfill.StatusCode);
        Assert.Equal("Completed", (await fulfill.Content.ReadFromJsonAsync<RequestResponse>())!.Status);
    }

    [Fact]
    public async Task Approve_Helpdesk_ByManagerFromAnotherDepartment_Returns400()
    {
        // The department-scoped Manager invariant (IDOR) holds for the helpdesk chain too.
        var id = await CreateSubmittedHelpdeskRequestAsync();
        var salesManager = factory.CreateClientWithToken(await factory.GetTokenAsync("manager.sales"));

        var response = await salesManager.PostAsJsonAsync($"/requests/{id}/approve", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Approve_Helpdesk_ByFinance_Returns400()
    {
        // DecideRequests admits Finance, but the helpdesk step requires Manager — the domain
        // rejects the wrong-role actor, so the shorter chain is no privilege-escalation path.
        var id = await CreateSubmittedHelpdeskRequestAsync();
        var finance = factory.CreateClientWithToken(await factory.GetTokenAsync("finance.user"));

        var response = await finance.PostAsJsonAsync($"/requests/{id}/approve", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_Helpdesk_RecordsManagerOnlyFlow()
    {
        var id = await CreateSubmittedHelpdeskRequestAsync();
        var manager = factory.CreateClientWithToken(await factory.GetTokenAsync("manager.eng"));
        await manager.PostAsJsonAsync($"/requests/{id}/approve", new { note = "ok" });
        var itAdmin = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));
        await itAdmin.PostAsync($"/requests/{id}/fulfill", content: null);

        var auditor = factory.CreateClientWithToken(await factory.GetTokenAsync("auditor.user"));
        var response = await auditor.GetAsync("/audit-logs");
        var entries = (await response.Content.ReadFromJsonAsync<List<AuditLogResponse>>())!
            .Where(e => e.TargetId == id)
            .Select(e => e.Action)
            .ToList();

        Assert.Contains("RequestCreated", entries);
        Assert.Contains("RequestSubmitted", entries);
        Assert.Contains("RequestFulfilled", entries);
        // Manager-only chain: exactly one approval recorded — no phantom Finance step.
        Assert.Equal(1, entries.Count(a => a == "RequestApproved"));
    }

    [Fact]
    public async Task GetAuditLogs_AsEmployee_Returns403()
    {
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));

        var response = await employee.GetAsync("/audit-logs");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_AsItAdmin_Returns200()
    {
        var itAdmin = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));

        var response = await itAdmin.GetAsync("/audit-logs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // operators read the trail (ReadAuditLog: Auditor + IT Admin)
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

    [Fact]
    public async Task Create_Helpdesk_WithPriorityAndCategory_RoundTrips()
    {
        var client = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));

        var response = await client.PostAsJsonAsync(
            "/requests",
            new { title = "VPN access", type = "Helpdesk", priority = "High", category = "AccessRequest" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<RequestResponse>();
        Assert.Equal("High", created!.Priority);
        Assert.Equal("AccessRequest", created.Category);
    }

    [Fact]
    public async Task Create_NonHelpdesk_WithCategory_Returns400()
    {
        var client = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));

        // Category is helpdesk-only; the domain rejects it on a procurement request.
        var response = await client.PostAsJsonAsync(
            "/requests",
            new { title = "Laptop", type = "Procurement", category = "Incident" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddComment_AsAuditor_Returns403()
    {
        var id = await CreateDraftRequestAsync();
        var auditor = factory.CreateClientWithToken(await factory.GetTokenAsync("auditor.user"));

        var response = await auditor.PostAsJsonAsync($"/requests/{id}/comments", new { body = "looks fine" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); // Auditor is read-only
    }

    [Fact]
    public async Task AddComment_AsEmployee_Returns201_AndAppearsInThread()
    {
        var id = await CreateDraftRequestAsync();
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));

        var post = await employee.PostAsJsonAsync($"/requests/{id}/comments", new { body = "Adding context." });
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        var created = await post.Content.ReadFromJsonAsync<CommentResponse>();
        Assert.Equal(EmployeeEngSub, created!.AuthorId); // author derived from the JWT, not the body

        var thread = await employee.GetFromJsonAsync<List<CommentResponse>>($"/requests/{id}/comments");
        Assert.Single(thread!);
        Assert.Equal("Adding context.", thread![0].Body);
    }

    [Fact]
    public async Task GetComments_AsAuditor_Returns200()
    {
        var id = await CreateDraftRequestAsync();
        var auditor = factory.CreateClientWithToken(await factory.GetTokenAsync("auditor.user"));

        var response = await auditor.GetAsync($"/requests/{id}/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // Auditor may read the thread
    }

    [Fact]
    public async Task AddComment_OnMissingRequest_Returns404()
    {
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));

        var response = await employee.PostAsJsonAsync($"/requests/{Guid.NewGuid()}/comments", new { body = "hi" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetComments_OnMissingRequest_Returns404()
    {
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));

        var response = await employee.GetAsync($"/requests/{Guid.NewGuid()}/comments");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    private async Task<Guid> CreateSubmittedHelpdeskRequestAsync()
    {
        var client = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));
        var created = await client.PostAsJsonAsync("/requests", new { title = "VPN access", type = "Helpdesk" });
        created.EnsureSuccessStatusCode();
        var id = (await created.Content.ReadFromJsonAsync<RequestResponse>())!.Id;
        (await client.PostAsync($"/requests/{id}/submit", content: null)).EnsureSuccessStatusCode();
        return id;
    }

    private sealed record RequestResponse(Guid Id, string Status, string Priority, string? Category, Guid RequesterId, Guid DepartmentId);

    private sealed record AuditLogResponse(string Action, Guid TargetId, string? FromStatus, string? ToStatus);

    private sealed record CommentResponse(Guid Id, Guid RequestId, Guid AuthorId, string Body, DateTimeOffset CreatedAtUtc);
}
