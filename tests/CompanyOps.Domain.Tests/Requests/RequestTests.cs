using CompanyOps.Domain.Common;
using CompanyOps.Domain.Requests;

namespace CompanyOps.Domain.Tests.Requests;

/// <summary>
/// Covers the Phase 1 creation invariants of the <see cref="Request"/> aggregate.
/// The factory enforces these in the Domain and throws <see cref="DomainException"/>;
/// these tests pin that behaviour. The state-machine transitions arrive in Phase 2.
/// </summary>
public class RequestTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Requester = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_WithValidInput_ReturnsDraftRequestWithFieldsSet()
    {
        var request = Request.Create("New laptop", "MacBook Pro 14", RequestType.Procurement, Requester, NowUtc);

        Assert.NotEqual(Guid.Empty, request.Id);
        Assert.Equal("New laptop", request.Title);
        Assert.Equal("MacBook Pro 14", request.Description);
        Assert.Equal(RequestType.Procurement, request.Type);
        Assert.Equal(RequestStatus.Draft, request.Status);
        Assert.Equal(Requester, request.RequesterId);
        Assert.Equal(NowUtc, request.CreatedAtUtc);
    }

    [Fact]
    public void Create_TrimsTitleAndDescription()
    {
        var request = Request.Create("  New laptop  ", "  spec  ", RequestType.Procurement, Requester, NowUtc);

        Assert.Equal("New laptop", request.Title);
        Assert.Equal("spec", request.Description);
    }

    [Fact]
    public void Create_WithNullDescription_IsAllowed()
    {
        var request = Request.Create("New laptop", null, RequestType.Procurement, Requester, NowUtc);

        Assert.Null(request.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingTitle_ThrowsDomainException(string? title)
    {
        var ex = Assert.Throws<DomainException>(
            () => Request.Create(title!, null, RequestType.Procurement, Requester, NowUtc));

        Assert.Equal("Request title is required.", ex.Message);
    }

    [Fact]
    public void Create_WithTitleExceedingMaxLength_ThrowsDomainException()
    {
        var tooLong = new string('a', Request.TitleMaxLength + 1);

        Assert.Throws<DomainException>(
            () => Request.Create(tooLong, null, RequestType.Procurement, Requester, NowUtc));
    }

    [Fact]
    public void Create_WithTitleAtMaxLength_IsAllowed()
    {
        var atLimit = new string('a', Request.TitleMaxLength);

        var request = Request.Create(atLimit, null, RequestType.Procurement, Requester, NowUtc);

        Assert.Equal(atLimit, request.Title);
    }

    [Fact]
    public void Create_WithEmptyRequesterId_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(
            () => Request.Create("New laptop", null, RequestType.Procurement, Guid.Empty, NowUtc));

        Assert.Equal("Request must have a requester.", ex.Message);
    }
}
