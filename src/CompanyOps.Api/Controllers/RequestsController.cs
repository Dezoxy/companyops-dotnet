using CompanyOps.Api.Auth;
using CompanyOps.Api.Contracts;
using CompanyOps.Application.Requests;
using CompanyOps.Application.Requests.ApproveRequest;
using CompanyOps.Application.Requests.CreateRequest;
using CompanyOps.Application.Requests.FulfillRequest;
using CompanyOps.Application.Requests.GetRequest;
using CompanyOps.Application.Requests.ListRequests;
using CompanyOps.Application.Requests.RejectRequest;
using CompanyOps.Application.Requests.SubmitRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyOps.Api.Controllers;

/// <summary>
/// Requests endpoints. Thin: each action authenticates, authorizes (role policy),
/// derives the actor from the JWT principal, dispatches to an Application handler,
/// and maps the result to HTTP. The fine-grained rules (department scope, workflow
/// stage, submit-own) are enforced in the Domain — see docs/security.md.
/// </summary>
[ApiController]
[Route("requests")]
[Authorize] // all endpoints require an authenticated principal; policies narrow further
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class RequestsController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Policies.CreateRequests)]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RequestDto>> Create(
        [FromBody] CreateRequestRequest body,
        [FromServices] CreateRequestHandler handler,
        CancellationToken cancellationToken)
    {
        // Requester and department come from the authenticated principal, never the body.
        var command = new CreateRequestCommand(body.Title, body.Description, body.Type, User.GetUserId(), User.GetDepartmentId());
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

    // Phase 5: when submission triggers async worker processing, return 202 Accepted.
    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = Policies.SubmitRequests)]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestDto>> Submit(
        Guid id,
        [FromServices] SubmitRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new SubmitRequestCommand(id, User.GetUserId()), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Policies.DecideRequests)]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestDto>> Approve(
        Guid id,
        [FromBody] ApproveRequestBody body,
        [FromServices] ApproveRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new ApproveRequestCommand(id, User.GetUserId(), User.GetApproverRole(), User.GetDepartmentId(), body.Note);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = Policies.DecideRequests)]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestDto>> Reject(
        Guid id,
        [FromBody] RejectRequestBody body,
        [FromServices] RejectRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectRequestCommand(id, User.GetUserId(), User.GetApproverRole(), User.GetDepartmentId(), body.Reason);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/fulfill")]
    [Authorize(Policy = Policies.FulfillRequests)]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestDto>> Fulfill(
        Guid id,
        [FromServices] FulfillRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new FulfillRequestCommand(id, User.GetUserId()), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
