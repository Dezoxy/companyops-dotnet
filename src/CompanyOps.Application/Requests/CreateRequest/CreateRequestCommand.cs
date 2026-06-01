using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests.CreateRequest;

/// <summary>
/// Use-case input for creating a new request. One command per business action;
/// this is the start of the "vertical slice per use case" convention.
/// </summary>
/// <remarks>
/// <paramref name="RequesterId"/> and <paramref name="DepartmentId"/> are derived from the
/// authenticated principal (the Keycloak token) in the Api and passed in here — never taken
/// from the request body.
/// <para><paramref name="Type"/> is nullable so an <em>omitted</em> type in the request body is
/// distinguishable from a valid value: the validator rejects null (a missing required field)
/// instead of letting a non-nullable enum silently default to <c>Procurement</c>.</para>
/// </remarks>
public sealed record CreateRequestCommand(
    string Title,
    string? Description,
    RequestType? Type,
    RequestPriority? Priority,
    RequestCategory? Category,
    Guid RequesterId,
    Guid DepartmentId);
