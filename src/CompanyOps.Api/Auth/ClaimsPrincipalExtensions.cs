using System.Security.Claims;
using CompanyOps.Domain.Requests;

namespace CompanyOps.Api.Auth;

/// <summary>
/// Reads the actor's identity from the authenticated principal (the JWT from Keycloak).
/// This is the Phase 3 replacement for the temporary request-body identity: the
/// controller derives who is acting from the token, never from the client payload.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>The Keycloak user id (<c>sub</c>) as the actor's id.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id)
            ? id
            : throw new InvalidOperationException("Authenticated principal has no usable 'sub' claim.");
    }

    /// <summary>The actor's department, from the custom <c>department</c> claim.</summary>
    public static Guid GetDepartmentId(this ClaimsPrincipal principal)
    {
        var dept = principal.FindFirstValue("department");
        return Guid.TryParse(dept, out var id)
            ? id
            : throw new InvalidOperationException("Authenticated principal has no usable 'department' claim.");
    }

    /// <summary>
    /// The approver role the actor is acting as (the first role that maps to an
    /// <see cref="ApproverRole"/>). Endpoint policies guarantee an approver-capable role
    /// is present; the Domain then checks it against the current step.
    /// </summary>
    public static ApproverRole GetApproverRole(this ClaimsPrincipal principal)
    {
        foreach (var role in principal.FindAll(ClaimTypes.Role))
        {
            if (Enum.TryParse<ApproverRole>(role.Value, ignoreCase: true, out var approverRole))
            {
                return approverRole;
            }
        }

        throw new InvalidOperationException("Authenticated principal holds no approver role.");
    }
}
