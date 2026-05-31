using CompanyOps.Api.Auth;
using CompanyOps.Api.Contracts;
using CompanyOps.Application.Requests;
using CompanyOps.Application.Requests.ApproveRequest;
using CompanyOps.Application.Requests.CancelRequest;
using CompanyOps.Application.Requests.Comments;
using CompanyOps.Application.Requests.Comments.AddComment;
using CompanyOps.Application.Requests.Comments.ListComments;
using CompanyOps.Application.Requests.CreateRequest;
using CompanyOps.Application.Requests.FulfillRequest;
using CompanyOps.Application.Requests.GetRequest;
using CompanyOps.Application.Requests.ListRequests;
using CompanyOps.Application.Requests.RejectRequest;
using CompanyOps.Application.Requests.SubmitRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
        // Priority/category pass through as-is; the Application layer applies the default.
        var command = new CreateRequestCommand(
            body.Title,
            body.Description,
            body.Type,
            body.Priority,
            body.Category,
            User.GetUserId(),
            User.GetDepartmentId());
        var created = await handler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RequestDto>>> List(
        [FromServices] ListRequestsHandler handler,
        CancellationToken cancellationToken)
    {
        var (requesterId, departmentId) = ReadScope();
        var result = await handler.HandleAsync(new ListRequestsQuery(requesterId, departmentId), cancellationToken);
        return Ok(result);
    }

    // Read scope from the principal's role (docs/security.md): the global/oversight roles see
    // everything; a Manager sees their department (what they can act on); an Employee only their
    // own. Shared by the list and the single-request read; the API is the authority.
    private (Guid? RequesterId, Guid? DepartmentId) ReadScope()
    {
        if (User.IsInRole(Roles.Finance) || User.IsInRole(Roles.ItAdmin) || User.IsInRole(Roles.Auditor))
        {
            return (null, null);
        }

        return User.IsInRole(Roles.Manager)
            ? (null, User.GetDepartmentId())
            : (User.GetUserId(), null);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestDto>> GetById(
        Guid id,
        [FromServices] GetRequestByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var (requesterId, departmentId) = ReadScope();
        var result = await handler.HandleAsync(new GetRequestByIdQuery(id, requesterId, departmentId), cancellationToken);
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

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Policies.CancelRequests)]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestDto>> Cancel(
        Guid id,
        [FromServices] CancelRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CancelRequestCommand(id, User.GetUserId()), cancellationToken);
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
        var command = new ApproveRequestCommand(id, User.GetUserId(), User.GetApproverRoles(), User.GetDepartmentId(), body.Note);
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
        var command = new RejectRequestCommand(id, User.GetUserId(), User.GetApproverRoles(), User.GetDepartmentId(), body.Reason);
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
        // Optional: only asset-lifecycle fulfillment carries a body; helpdesk/procurement POST none.
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] FulfillRequestBody? body,
        CancellationToken cancellationToken)
    {
        var command = new FulfillRequestCommand(id, User.GetUserId(), body?.AssignedAssetId);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/comments")]
    [Authorize(Policy = Policies.CommentOnRequests)] // any participant; the read-only Auditor is excluded
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> AddComment(
        Guid id,
        [FromBody] AddCommentRequest body,
        [FromServices] AddCommentHandler handler,
        CancellationToken cancellationToken)
    {
        // The author comes from the JWT principal, never the body.
        var result = await handler.HandleAsync(new AddCommentCommand(id, User.GetUserId(), body.Body), cancellationToken);
        return result is null ? NotFound() : CreatedAtAction(nameof(GetComments), new { id }, result);
    }

    // Any authenticated principal may read the thread, including the Auditor (read-only).
    // Explicit [Authorize] (redundant with the class-level one) so narrowing the class
    // default can never silently expose this read.
    [HttpGet("{id:guid}/comments")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetComments(
        Guid id,
        [FromServices] ListCommentsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListCommentsQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
