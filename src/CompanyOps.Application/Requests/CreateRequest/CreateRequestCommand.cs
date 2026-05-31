using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests.CreateRequest;

/// <summary>
/// Use-case input for creating a new request. One command per business action;
/// this is the start of the "vertical slice per use case" convention.
/// </summary>
/// <remarks>
/// <paramref name="RequesterId"/> and <paramref name="DepartmentId"/> are supplied by
/// the caller for now because there is no authentication yet. From Phase 3 they come
/// from the authenticated principal (the Keycloak token), not the request body.
/// </remarks>
public sealed record CreateRequestCommand(
    string Title,
    string? Description,
    RequestType Type,
    RequestPriority? Priority,
    RequestCategory? Category,
    Guid RequesterId,
    Guid DepartmentId);
