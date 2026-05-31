using CompanyOps.Domain.Requests;

namespace CompanyOps.Api.Contracts;

/// <summary>
/// API request body for creating a request. Kept separate from the Application
/// command so the HTTP contract can evolve independently.
/// </summary>
/// <remarks>
/// Requester and department are taken from the authenticated principal (the JWT),
/// not the body — the client cannot assert who it is or which department it belongs to.
/// </remarks>
public sealed record CreateRequestRequest(
    string Title,
    string? Description,
    RequestType Type,
    RequestPriority? Priority,
    RequestCategory? Category);
