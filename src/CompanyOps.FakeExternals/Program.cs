// Mock external enterprise systems (Phase 6): a Finance "budget commitment" API and an
// Inventory "asset reservation" API. Real HTTP so the integration and its failure modes
// are exercised for real. Each endpoint can be forced to fail via config
// (FakeExternals:FailFinance / FailInventory) to demonstrate retry / graceful handling.

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var failFinance = builder.Configuration.GetValue<bool>("FakeExternals:FailFinance");
var failInventory = builder.Configuration.GetValue<bool>("FakeExternals:FailInventory");

app.MapGet("/health", () => Results.Ok("ok"));

app.MapPost("/finance/commitments", (BudgetCommitRequest request) =>
    failFinance
        ? Results.Problem("Finance system temporarily unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable)
        : Results.Ok(new BudgetCommitResponse(Guid.NewGuid(), "Committed")));

app.MapPost("/inventory/reservations", (AssetReservationRequest request) =>
    failInventory
        ? Results.Problem("Inventory system temporarily unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable)
        : Results.Ok(new AssetReservationResponse(Guid.NewGuid(), "Reserved")));

app.Run();

internal sealed record BudgetCommitRequest(Guid RequestId, Guid DepartmentId);
internal sealed record BudgetCommitResponse(Guid CommitmentId, string Status);
internal sealed record AssetReservationRequest(Guid RequestId);
internal sealed record AssetReservationResponse(Guid ReservationId, string Status);

// Exposed so an integration test can host the mock with WebApplicationFactory<Program>.
public partial class Program;
