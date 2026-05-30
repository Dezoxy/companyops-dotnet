using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests;

/// <summary>
/// Read model returned across the Application boundary. Domain entities are never
/// exposed directly (see AGENTS.md conventions); handlers map to this DTO.
/// </summary>
public sealed record RequestDto(
    Guid Id,
    string Title,
    string? Description,
    RequestType Type,
    RequestStatus Status,
    Guid RequesterId,
    DateTimeOffset CreatedAtUtc)
{
    public static RequestDto FromDomain(Request request) => new(
        request.Id,
        request.Title,
        request.Description,
        request.Type,
        request.Status,
        request.RequesterId,
        request.CreatedAtUtc);
}
