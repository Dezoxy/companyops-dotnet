using CompanyOps.Api.Contracts;
using CompanyOps.Application.Requests;
using CompanyOps.Application.Requests.CreateRequest;
using CompanyOps.Application.Requests.GetRequest;
using CompanyOps.Application.Requests.ListRequests;
using Microsoft.AspNetCore.Mvc;

namespace CompanyOps.Api.Controllers;

/// <summary>
/// Requests endpoints. Thin: each action authenticates (from Phase 3), binds the
/// DTO, dispatches to an Application handler, and maps the result to HTTP. No
/// business logic here.
/// </summary>
[ApiController]
[Route("requests")]
public sealed class RequestsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RequestDto>> Create(
        [FromBody] CreateRequestRequest body,
        [FromServices] CreateRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateRequestCommand(body.Title, body.Description, body.Type, body.RequesterId);
        var created = await handler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RequestDto>>> List(
        [FromServices] ListRequestsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListRequestsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestDto>> GetById(
        Guid id,
        [FromServices] GetRequestByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetRequestByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
