using CompanyOps.Api.Auth;
using CompanyOps.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyOps.Api.Controllers;

/// <summary>
/// Reports & Analytics (Phase 18) — read-only aggregate counts for the oversight roles
/// (<see cref="Policies.ReadReports"/>: Manager / Finance / IT Admin / Auditor). The numbers are
/// aggregated in the database (see ReportingStore), and are <em>global</em> — not department-scoped;
/// department-scoped reporting is a documented enterprise follow-up (docs/security.md). Thin:
/// authorize, dispatch to a handler, return the DTO.
/// </summary>
[ApiController]
[Route("reports")]
[Authorize(Policy = Policies.ReadReports)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ReportsController : ControllerBase
{
    [HttpGet("requests")]
    [ProducesResponseType(typeof(RequestReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RequestReportDto>> Requests(
        [FromServices] GetRequestReportHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(new GetRequestReportQuery(), cancellationToken));

    [HttpGet("assets")]
    [ProducesResponseType(typeof(AssetReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssetReportDto>> Assets(
        [FromServices] GetAssetReportHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(new GetAssetReportQuery(), cancellationToken));
}
