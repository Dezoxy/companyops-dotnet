namespace CompanyOps.Domain.Requests;

/// <summary>
/// The <em>template</em> for one approval step — part of a request type's chain in
/// <see cref="ApprovalChains"/>. It is pure configuration: it carries no decision.
/// When a request is submitted, each definition is materialized into an
/// <see cref="ApprovalStep"/> instance on the request.
/// </summary>
/// <param name="Order">Position in the chain, ascending from 1.</param>
/// <param name="RequiredRole">The role allowed to decide this step.</param>
/// <param name="Scope">How the approver is matched (department-scoped or global).</param>
/// <param name="IsRequired">
/// Whether the step must be approved for the request to reach
/// <see cref="RequestStatus.Approved"/>. Optional steps do not block approval.
/// </param>
public sealed record ApprovalStepDefinition(
    int Order,
    ApproverRole RequiredRole,
    ApprovalScope Scope,
    bool IsRequired);
