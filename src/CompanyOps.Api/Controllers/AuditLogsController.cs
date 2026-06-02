using CompanyOps.Api.Auth;
using CompanyOps.Application.Auditing;
using CompanyOps.Application.Auditing.ListAuditLogs;
using CompanyOps.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyOps.Api.Controllers;

/// <summary>
/// Read-only audit trail. There is no write/update/delete endpoint — the log is
/// append-only and is written as a side effect of business actions, never via the API.
/// </summary>
[ApiController]
[Route("audit-logs")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AuditLogsController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.ReadAuditLog)]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> List(
        [FromServices] ListAuditLogsHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        var result = await handler.HandleAsync(new ListAuditLogsQuery(new PageRequest(page, pageSize)), cancellationToken);
        return Ok(result);
    }
}
