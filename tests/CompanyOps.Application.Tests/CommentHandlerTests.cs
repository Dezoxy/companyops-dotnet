using CompanyOps.Application.Requests.Comments.AddComment;
using CompanyOps.Application.Requests.Comments.ListComments;
using CompanyOps.Domain.Requests;
using Xunit;

namespace CompanyOps.Application.Tests;

/// <summary>
/// Fast Application-layer tests for the comment handlers — the request-existence guard and the
/// null-vs-empty distinction (missing request → null/404; existing request with no comments →
/// empty list). Previously only exercised through the HTTP integration path. Fakes, no database.
/// </summary>
public class CommentHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Requester = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Department = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Author = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly FakeRequestRepository _requests = new();
    private readonly FakeCommentRepository _comments = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FixedTimeProvider _clock = new(Now);

    private Request SeedRequest()
    {
        var request = Request.Create("Laptop", null, RequestType.Procurement, RequestPriority.Medium, null, Requester, Department, Now);
        _requests.Seed(request);
        return request;
    }

    [Fact]
    public async Task AddComment_ForExistingRequest_PersistsAndReturnsDto()
    {
        var request = SeedRequest();
        var handler = new AddCommentHandler(_requests, _comments, _uow, _clock);

        var dto = await handler.HandleAsync(new AddCommentCommand(request.Id, Author, "Looks good to me"));

        Assert.NotNull(dto);
        Assert.Equal("Looks good to me", dto!.Body);
        Assert.Equal(Author, dto.AuthorId);
        Assert.Single(_comments.Store);
        Assert.Equal(1, _uow.SaveCount);
    }

    [Fact]
    public async Task AddComment_MissingRequest_ReturnsNull_DoesNotPersist()
    {
        var handler = new AddCommentHandler(_requests, _comments, _uow, _clock);

        var dto = await handler.HandleAsync(new AddCommentCommand(Guid.NewGuid(), Author, "orphan"));

        Assert.Null(dto);
        Assert.Empty(_comments.Store);
        Assert.Equal(0, _uow.SaveCount);
    }

    [Fact]
    public async Task ListComments_MissingRequest_ReturnsNull()
    {
        var handler = new ListCommentsHandler(_requests, _comments);

        var result = await handler.HandleAsync(new ListCommentsQuery(Guid.NewGuid()));

        Assert.Null(result); // null → 404, distinct from an empty thread
    }

    [Fact]
    public async Task ListComments_ExistingRequestWithNoComments_ReturnsEmptyList()
    {
        var request = SeedRequest();
        var handler = new ListCommentsHandler(_requests, _comments);

        var result = await handler.HandleAsync(new ListCommentsQuery(request.Id));

        Assert.NotNull(result); // not null — the request exists
        Assert.Empty(result!);
    }

    [Fact]
    public async Task ListComments_ReturnsTheRequestsThread()
    {
        var request = SeedRequest();
        var add = new AddCommentHandler(_requests, _comments, _uow, _clock);
        await add.HandleAsync(new AddCommentCommand(request.Id, Author, "first"));
        await add.HandleAsync(new AddCommentCommand(request.Id, Author, "second"));
        var handler = new ListCommentsHandler(_requests, _comments);

        var result = await handler.HandleAsync(new ListCommentsQuery(request.Id));

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal(["first", "second"], result.Select(c => c.Body));
    }
}
