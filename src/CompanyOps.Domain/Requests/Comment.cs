using CompanyOps.Domain.Common;

namespace CompanyOps.Domain.Requests;

/// <summary>
/// A note on a request's discussion thread. A separate aggregate from <see cref="Request"/>
/// (it grows independently of the request's state and is loaded on demand), referencing the
/// request by id. Append-only: there is no edit or delete — author + timestamp make each
/// comment its own immutable record, so the thread is audit-grade without a separate audit entry.
/// </summary>
public sealed class Comment
{
    public const int BodyMaxLength = 4000;

    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Body { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    // Required by EF Core's materializer; not for application use.
    private Comment()
    {
    }

    private Comment(Guid id, Guid requestId, Guid authorId, string body, DateTimeOffset createdAtUtc)
    {
        Id = id;
        RequestId = requestId;
        AuthorId = authorId;
        Body = body;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Factory for a new comment. Enforces the invariants in the Domain (throws
    /// <see cref="DomainException"/>) rather than trusting the caller.
    /// </summary>
    public static Comment Create(Guid requestId, Guid authorId, string body, DateTimeOffset nowUtc)
    {
        if (requestId == Guid.Empty)
        {
            throw new DomainException("A comment must belong to a request.");
        }

        if (authorId == Guid.Empty)
        {
            throw new DomainException("A comment must record its author.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new DomainException("Comment body is required.");
        }

        body = body.Trim();
        if (body.Length > BodyMaxLength)
        {
            throw new DomainException($"Comment body must be at most {BodyMaxLength} characters.");
        }

        return new Comment(Guid.NewGuid(), requestId, authorId, body, nowUtc);
    }
}
