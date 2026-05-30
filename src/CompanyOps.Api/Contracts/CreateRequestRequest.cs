using CompanyOps.Domain.Requests;

namespace CompanyOps.Api.Contracts;

/// <summary>
/// API request body for creating a request. Kept separate from the Application
/// command so the HTTP contract can evolve independently.
/// </summary>
/// <remarks>
/// <see cref="RequesterId"/> is accepted from the client only because there is no
/// authentication yet (Phase 1). From Phase 3 it is taken from the authenticated
/// principal and removed from this body.
/// </remarks>
public sealed record CreateRequestRequest(
    string Title,
    string? Description,
    RequestType Type,
    Guid RequesterId);
