namespace CompanyOps.Api.Auth;

/// <summary>
/// Role names (as they appear in Keycloak realm roles) and the authorization policy
/// names used to gate endpoints. Policies are the coarse, role-based gate at the API
/// boundary; the fine-grained department-scope and workflow-stage rules are enforced
/// in the Domain (defense in depth — see docs/security.md).
/// </summary>
internal static class Roles
{
    public const string Employee = "Employee";
    public const string Manager = "Manager";
    public const string Finance = "Finance";
    public const string ItAdmin = "ItAdmin";
    public const string Auditor = "Auditor";
}

internal static class Policies
{
    /// <summary>Create and submit a request — the requester acts as an Employee.</summary>
    public const string CreateRequests = "CreateRequests";
    public const string SubmitRequests = "SubmitRequests";

    /// <summary>Approve or reject the current step — Manager or Finance (the domain
    /// matches the actor's role to the specific step).</summary>
    public const string DecideRequests = "DecideRequests";

    /// <summary>Fulfill an approved request — IT Admin.</summary>
    public const string FulfillRequests = "FulfillRequests";

    /// <summary>Comment on a request — any participant (everyone except the read-only Auditor).</summary>
    public const string CommentOnRequests = "CommentOnRequests";

    /// <summary>Read the audit trail — Auditor (matrix lists IT Admin as a TODO).</summary>
    public const string ReadAuditLog = "ReadAuditLog";

    /// <summary>Manage the asset inventory and its lifecycle (write) — IT Admin.</summary>
    public const string ManageAssets = "ManageAssets";

    /// <summary>Read the asset inventory + history — IT Admin and the read-only Auditor.</summary>
    public const string ReadAssets = "ReadAssets";

    /// <summary>Read aggregate reports / analytics — the oversight roles (Manager, Finance, IT
    /// Admin, Auditor). Plain Employees see only their own requests, not org-wide analytics.</summary>
    public const string ReadReports = "ReadReports";

    /// <summary>Read the integration pipeline's operational status (outbox/worker) — IT Admin
    /// (operators) and the read-only Auditor (oversight). Business roles don't see plumbing.</summary>
    public const string ReadIntegrations = "ReadIntegrations";
}
