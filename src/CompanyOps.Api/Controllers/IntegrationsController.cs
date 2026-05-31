using CompanyOps.Api.Auth;
using CompanyOps.Application.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyOps.Api.Controllers;

/// <summary>
/// Integration pipeline status (Phase 19) — a read-only operational view over the transactional
/// outbox and the Worker's processed-message markers (ADR 0007/0008). Gated by
/// <see cref="Policies.ReadIntegrations"/> (IT Admin + Auditor): it is plumbing/observability, not
/// a business view. Thin: authorize, dispatch to a handler, return the DTO.
/// </summary>
[ApiController]
[Route("integrations")]
[Authorize(Policy = Policies.ReadIntegrations)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class IntegrationsController : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType(typeof(IntegrationStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<IntegrationStatusDto>> Status(
        [FromServices] GetIntegrationStatusHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(new GetIntegrationStatusQuery(), cancellationToken));
}
