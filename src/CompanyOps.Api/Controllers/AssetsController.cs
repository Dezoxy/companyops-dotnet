using CompanyOps.Api.Auth;
using CompanyOps.Api.Contracts;
using CompanyOps.Application.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyOps.Api.Controllers;

/// <summary>
/// Asset inventory + lifecycle — the IT-Admin console. Thin: authenticate, authorize, derive
/// the actor from the JWT, dispatch to a handler, map to HTTP. Reads are open to IT Admin and
/// the read-only Auditor (ReadAssets); writes are IT-Admin-only (ManageAssets). The lifecycle
/// invariants live in the Asset aggregate; each transition is audited (the asset's history).
/// </summary>
[ApiController]
[Route("assets")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AssetsController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.ReadAssets)]
    [ProducesResponseType(typeof(IReadOnlyList<AssetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssetDto>>> List(
        [FromServices] ListAssetsHandler handler,
        CancellationToken cancellationToken) => Ok(await handler.HandleAsync(new ListAssetsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.ReadAssets)]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> GetById(
        Guid id,
        [FromServices] GetAssetByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAssetByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/history")]
    [Authorize(Policy = Policies.ReadAssets)]
    [ProducesResponseType(typeof(IReadOnlyList<AssetHistoryEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AssetHistoryEntryDto>>> History(
        Guid id,
        [FromServices] GetAssetHistoryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAssetHistoryQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.ManageAssets)]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)] // tag already in use
    public async Task<ActionResult<AssetDto>> Register(
        [FromBody] RegisterAssetRequest body,
        [FromServices] RegisterAssetHandler handler,
        CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(
            new RegisterAssetCommand(body.Tag, body.Name, body.Type, User.GetUserId()),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Policy = Policies.ManageAssets)]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> Assign(
        Guid id,
        [FromBody] AssignAssetRequest body,
        [FromServices] AssignAssetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AssignAssetCommand(id, body.UserId, User.GetUserId()), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/reclaim")]
    [Authorize(Policy = Policies.ManageAssets)]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> Reclaim(
        Guid id,
        [FromServices] ReclaimAssetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AssetTransitionCommand(id, User.GetUserId()), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/repair")]
    [Authorize(Policy = Policies.ManageAssets)]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> SendToRepair(
        Guid id,
        [FromServices] SendAssetToRepairHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AssetTransitionCommand(id, User.GetUserId()), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/return-from-repair")]
    [Authorize(Policy = Policies.ManageAssets)]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> ReturnFromRepair(
        Guid id,
        [FromServices] ReturnAssetFromRepairHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AssetTransitionCommand(id, User.GetUserId()), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/retire")]
    [Authorize(Policy = Policies.ManageAssets)]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> Retire(
        Guid id,
        [FromServices] RetireAssetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AssetTransitionCommand(id, User.GetUserId()), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
