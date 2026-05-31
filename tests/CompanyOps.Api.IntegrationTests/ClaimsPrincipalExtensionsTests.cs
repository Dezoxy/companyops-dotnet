using System.Security.Claims;
using CompanyOps.Api.Auth;
using CompanyOps.Domain.Requests;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// Unit tests for the principal claim readers (no host, no container). A valid-but-insufficient
/// token — one that passed JWT validation but lacks <c>sub</c> / <c>department</c> — raises
/// <see cref="MissingClaimException"/> (which the handler maps to 403), never an unhandled 500.
/// Also pins the multi-role mapping behind the Phase-2 follow-up fix.
/// </summary>
public sealed class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal PrincipalWith(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "test"));

    [Fact]
    public void GetUserId_WithoutSub_ThrowsMissingClaim()
    {
        var principal = PrincipalWith(new Claim("department", Guid.NewGuid().ToString()));

        var ex = Assert.Throws<MissingClaimException>(() => principal.GetUserId());

        Assert.Equal("sub", ex.Claim);
    }

    [Fact]
    public void GetDepartmentId_WithoutDepartment_ThrowsMissingClaim()
    {
        var principal = PrincipalWith(new Claim("sub", Guid.NewGuid().ToString()));

        var ex = Assert.Throws<MissingClaimException>(() => principal.GetDepartmentId());

        Assert.Equal("department", ex.Claim);
    }

    [Fact]
    public void GetUserId_AndGetDepartmentId_ReturnTheClaimValues_WhenPresent()
    {
        var sub = Guid.NewGuid();
        var department = Guid.NewGuid();
        var principal = PrincipalWith(new Claim("sub", sub.ToString()), new Claim("department", department.ToString()));

        Assert.Equal(sub, principal.GetUserId());
        Assert.Equal(department, principal.GetDepartmentId());
    }

    [Fact]
    public void GetApproverRoles_MapsApproverRoles_AndIgnoresOthers()
    {
        var principal = PrincipalWith(
            new Claim(ClaimTypes.Role, "Manager"),
            new Claim(ClaimTypes.Role, "Finance"),
            new Claim(ClaimTypes.Role, "Employee")); // not an approver role

        var roles = principal.GetApproverRoles();

        Assert.Contains(ApproverRole.Manager, roles);
        Assert.Contains(ApproverRole.Finance, roles);
        Assert.Equal(2, roles.Count); // Employee is ignored
    }
}
