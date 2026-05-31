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
    RequestPriority Priority,
    RequestCategory? Category,
    RequestStatus Status,
    Guid RequesterId,
    Guid DepartmentId,
    DateTimeOffset CreatedAtUtc,
    Guid? FulfilledAssetId,
    IReadOnlyList<ApprovalStepDto> ApprovalSteps)
{
    public static RequestDto FromDomain(Request request) => new(
        request.Id,
        request.Title,
        request.Description,
        request.Type,
        request.Priority,
        request.Category,
        request.Status,
        request.RequesterId,
        request.DepartmentId,
        request.CreatedAtUtc,
        request.FulfilledAssetId,
        [.. request.ApprovalSteps.Select(ApprovalStepDto.FromDomain)]);
}
