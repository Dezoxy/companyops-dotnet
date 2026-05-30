using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests;

/// <summary>
/// Read model for one approval step, exposed as part of <see cref="RequestDto"/> so a
/// client can see the chain and who is pending. Maps from the owned domain entity.
/// </summary>
public sealed record ApprovalStepDto(
    int Order,
    ApproverRole RequiredRole,
    ApprovalScope Scope,
    bool IsRequired,
    ApprovalDecision Decision,
    Guid? DecidedById,
    DateTimeOffset? DecidedAtUtc,
    string? Note)
{
    public static ApprovalStepDto FromDomain(ApprovalStep step) => new(
        step.Order,
        step.RequiredRole,
        step.Scope,
        step.IsRequired,
        step.Decision,
        step.DecidedById,
        step.DecidedAtUtc,
        step.Note);
}
