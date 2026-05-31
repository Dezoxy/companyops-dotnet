using CompanyOps.Domain.Common;
using CompanyOps.Domain.Requests;

namespace CompanyOps.Domain.Tests.Requests;

/// <summary>
/// Covers the creation invariants of the <see cref="Request"/> aggregate. The factory
/// enforces these in the Domain and throws <see cref="DomainException"/>; these tests
/// pin that behaviour. The approval state-machine transitions are covered in
/// <see cref="ApprovalWorkflowTests"/>.
/// </summary>
public class RequestTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Requester = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Department = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Create_WithValidInput_ReturnsDraftRequestWithFieldsSet()
    {
        var request = Request.Create("New laptop", "MacBook Pro 14", RequestType.Procurement, RequestPriority.Medium, null, Requester, Department, NowUtc);

        Assert.NotEqual(Guid.Empty, request.Id);
        Assert.Equal("New laptop", request.Title);
        Assert.Equal("MacBook Pro 14", request.Description);
        Assert.Equal(RequestType.Procurement, request.Type);
        Assert.Equal(RequestStatus.Draft, request.Status);
        Assert.Equal(Requester, request.RequesterId);
        Assert.Equal(Department, request.DepartmentId);
        Assert.Equal(NowUtc, request.CreatedAtUtc);
        Assert.Empty(request.ApprovalSteps);
    }

    [Fact]
    public void Create_TrimsTitleAndDescription()
    {
        var request = Request.Create("  New laptop  ", "  spec  ", RequestType.Procurement, RequestPriority.Medium, null, Requester, Department, NowUtc);

        Assert.Equal("New laptop", request.Title);
        Assert.Equal("spec", request.Description);
    }

    [Fact]
    public void Create_WithNullDescription_IsAllowed()
    {
        var request = Request.Create("New laptop", null, RequestType.Procurement, RequestPriority.Medium, null, Requester, Department, NowUtc);

        Assert.Null(request.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingTitle_ThrowsDomainException(string? title)
    {
        var ex = Assert.Throws<DomainException>(
            () => Request.Create(title!, null, RequestType.Procurement, RequestPriority.Medium, null, Requester, Department, NowUtc));

        Assert.Equal("Request title is required.", ex.Message);
    }

    [Fact]
    public void Create_WithTitleExceedingMaxLength_ThrowsDomainException()
    {
        var tooLong = new string('a', Request.TitleMaxLength + 1);

        Assert.Throws<DomainException>(
            () => Request.Create(tooLong, null, RequestType.Procurement, RequestPriority.Medium, null, Requester, Department, NowUtc));
    }

    [Fact]
    public void Create_WithTitleAtMaxLength_IsAllowed()
    {
        var atLimit = new string('a', Request.TitleMaxLength);

        var request = Request.Create(atLimit, null, RequestType.Procurement, RequestPriority.Medium, null, Requester, Department, NowUtc);

        Assert.Equal(atLimit, request.Title);
    }

    [Fact]
    public void Create_WithEmptyRequesterId_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(
            () => Request.Create("New laptop", null, RequestType.Procurement, RequestPriority.Medium, null, Guid.Empty, Department, NowUtc));

        Assert.Equal("Request must have a requester.", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyDepartmentId_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(
            () => Request.Create("New laptop", null, RequestType.Procurement, RequestPriority.Medium, null, Requester, Guid.Empty, NowUtc));

        Assert.Equal("Request must belong to a department.", ex.Message);
    }

    [Fact]
    public void Create_SetsPriority()
    {
        var request = Request.Create("New laptop", null, RequestType.Procurement, RequestPriority.High, null, Requester, Department, NowUtc);

        Assert.Equal(RequestPriority.High, request.Priority);
    }

    [Fact]
    public void Create_HelpdeskWithCategory_IsAllowed()
    {
        var request = Request.Create(
            "VPN access", null, RequestType.Helpdesk, RequestPriority.Medium, RequestCategory.AccessRequest, Requester, Department, NowUtc);

        Assert.Equal(RequestCategory.AccessRequest, request.Category);
    }

    [Fact]
    public void Create_NonHelpdeskWithCategory_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(
            () => Request.Create(
                "New laptop", null, RequestType.Procurement, RequestPriority.Medium, RequestCategory.Incident, Requester, Department, NowUtc));

        Assert.Contains("category", ex.Message);
    }
}
