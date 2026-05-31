using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// Integration pipeline status (Phase 19): the operational snapshot over the outbox + worker, and
/// its authorization. The integration DB is shared, so assertions are isolation-safe — internal
/// consistency (total == pending + published + failed), valid per-message status, and that an
/// approval this test triggers surfaces as an outbox message — never exact global totals.
/// </summary>
[Collection("Integration")]
public sealed class IntegrationsStatusTests(ApiFactory factory)
{
    [Fact]
    public async Task Status_AsEmployee_Returns403()
    {
        var employee = factory.CreateClientWithToken(await factory.GetTokenAsync("employee.eng"));

        var response = await employee.GetAsync("/integrations/status");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Status_AsManager_Returns403()
    {
        var manager = factory.CreateClientWithToken(await factory.GetTokenAsync("manager.eng"));

        var response = await manager.GetAsync("/integrations/status");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); // integration plumbing is not a business view
    }

    [Fact]
    public async Task Status_AsAuditor_Returns200()
    {
        var auditor = factory.CreateClientWithToken(await factory.GetTokenAsync("auditor.user"));

        var response = await auditor.GetAsync("/integrations/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // read-only Auditor oversees the pipeline
    }

    [Fact]
    public async Task Status_AfterApproval_ShowsTheOutboxMessage_WithConsistentTotals()
    {
        // Fully approving a request enqueues a RequestApproved event into the outbox.
        await factory.FullyApproveRequestAsync();
        var itAdmin = factory.CreateClientWithToken(await factory.GetTokenAsync("itadmin.user"));

        var status = await itAdmin.GetFromJsonAsync<IntegrationStatus>("/integrations/status");

        Assert.NotNull(status);
        Assert.True(status!.Outbox.Total >= 1);
        Assert.Equal(status.Outbox.Total, status.Outbox.Pending + status.Outbox.Published + status.Outbox.Failed);
        Assert.True(status.ProcessedByWorker >= 0);
        Assert.Contains(status.Recent, m => m.Type == "RequestApproved"); // the approval we just triggered
        Assert.All(status.Recent, m => Assert.Contains(m.Status, new[] { "Pending", "Published", "Failed" }));
    }

    private sealed record IntegrationStatus(OutboxSummary Outbox, int ProcessedByWorker, IReadOnlyList<IntegrationMessage> Recent);

    private sealed record OutboxSummary(int Total, int Pending, int Published, int Failed);

    private sealed record IntegrationMessage(
        Guid Id,
        string Type,
        string Status,
        DateTimeOffset OccurredAtUtc,
        DateTimeOffset? ProcessedAtUtc,
        int Attempts,
        string? Error);
}
