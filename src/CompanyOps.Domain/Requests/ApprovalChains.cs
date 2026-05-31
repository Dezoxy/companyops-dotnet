using CompanyOps.Domain.Common;

namespace CompanyOps.Domain.Requests;

/// <summary>
/// The configurable approval chains, keyed by <see cref="RequestType"/> (ADR 0005).
/// Seeded in code for the MVP; a DB-backed editor is enterprise-optional and out of
/// scope. This is the one place the procurement chain's shape lives — adding a new
/// internal process (helpdesk, asset lifecycle) is a new entry here plus a
/// fulfillment handler, not a new subsystem.
/// </summary>
public static class ApprovalChains
{
    // Each request type's chain lives here — the engine materializes it on Submit:
    //  - Procurement (Phase 2 seed): the requester's manager approves (department-scoped),
    //    then central Finance signs off (global).
    //  - Helpdesk (Phase 15): manager-only (department-scoped). Low-risk service/access
    //    requests need one sign-off, then IT fulfils — a deliberately different shape from
    //    procurement, which is the whole point of the configurable engine (ADR 0005).
    // Asset-lifecycle (Phase 16) is added with its flow.
    private static readonly IReadOnlyDictionary<RequestType, IReadOnlyList<ApprovalStepDefinition>> Chains =
        new Dictionary<RequestType, IReadOnlyList<ApprovalStepDefinition>>
        {
            [RequestType.Procurement] =
            [
                new ApprovalStepDefinition(1, ApproverRole.Manager, ApprovalScope.Department, IsRequired: true),
                new ApprovalStepDefinition(2, ApproverRole.Finance, ApprovalScope.Global, IsRequired: true),
            ],
            [RequestType.Helpdesk] =
            [
                new ApprovalStepDefinition(1, ApproverRole.Manager, ApprovalScope.Department, IsRequired: true),
            ],
        };

    /// <summary>
    /// The ordered approval chain for a request type. Throws if no chain is configured
    /// — a request type cannot be submitted until its flow exists (fail loud, not a
    /// silent auto-approve).
    /// </summary>
    public static IReadOnlyList<ApprovalStepDefinition> For(RequestType type) =>
        Chains.TryGetValue(type, out var chain)
            ? chain
            : throw new DomainException($"No approval chain is configured for request type '{type}'.");
}
