using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests.CreateRequest;

/// <summary>
/// Use-case input for creating a new request. One command per business action;
/// this is the start of the "vertical slice per use case" convention.
/// </summary>
/// <remarks>
/// <paramref name="RequesterId"/> is supplied by the caller in Phase 1 because
/// there is no authentication yet. From Phase 3 it comes from the authenticated
/// principal (the Keycloak token), not the request body.
/// </remarks>
public sealed record CreateRequestCommand(
    string Title,
    string? Description,
    RequestType Type,
    Guid RequesterId);
