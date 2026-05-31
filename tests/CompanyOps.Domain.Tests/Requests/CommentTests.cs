using CompanyOps.Domain.Common;
using CompanyOps.Domain.Requests;

namespace CompanyOps.Domain.Tests.Requests;

/// <summary>Creation invariants of the <see cref="Comment"/> aggregate (append-only thread note).</summary>
public class CommentTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid RequestId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AuthorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Create_WithValidInput_SetsFields()
    {
        var comment = Comment.Create(RequestId, AuthorId, "Please expedite.", Now);

        Assert.NotEqual(Guid.Empty, comment.Id);
        Assert.Equal(RequestId, comment.RequestId);
        Assert.Equal(AuthorId, comment.AuthorId);
        Assert.Equal("Please expedite.", comment.Body);
        Assert.Equal(Now, comment.CreatedAtUtc);
    }

    [Fact]
    public void Create_TrimsBody()
    {
        Assert.Equal("hello", Comment.Create(RequestId, AuthorId, "  hello  ", Now).Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankBody_ThrowsDomainException(string? body)
    {
        Assert.Throws<DomainException>(() => Comment.Create(RequestId, AuthorId, body!, Now));
    }

    [Fact]
    public void Create_WithBodyExceedingMaxLength_ThrowsDomainException()
    {
        var tooLong = new string('a', Comment.BodyMaxLength + 1);

        Assert.Throws<DomainException>(() => Comment.Create(RequestId, AuthorId, tooLong, Now));
    }

    [Fact]
    public void Create_WithEmptyRequestId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Comment.Create(Guid.Empty, AuthorId, "hi", Now));
    }

    [Fact]
    public void Create_WithEmptyAuthorId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Comment.Create(RequestId, Guid.Empty, "hi", Now));
    }
}
