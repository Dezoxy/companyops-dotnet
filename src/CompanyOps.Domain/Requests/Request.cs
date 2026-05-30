using CompanyOps.Domain.Common;

namespace CompanyOps.Domain.Requests;

/// <summary>
/// A request raised by an employee that flows through an approval → fulfillment
/// lifecycle. This is the aggregate root of the workflow engine.
/// <para>
/// Phase 1 scope: creation and persistence only. A request is born in
/// <see cref="RequestStatus.Draft"/>. The state-machine methods (Submit, Approve,
/// Reject, Fulfill, …) and their invariants are added in Phase 2.
/// </para>
/// </summary>
public class Request
{
    public const int TitleMaxLength = 200;

    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public RequestType Type { get; private set; }
    public RequestStatus Status { get; private set; }
    public Guid RequesterId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    // Required by EF Core's materializer; not for application use.
    private Request()
    {
    }

    private Request(
        Guid id,
        string title,
        string? description,
        RequestType type,
        Guid requesterId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        Title = title;
        Description = description;
        Type = type;
        Status = RequestStatus.Draft;
        RequesterId = requesterId;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Factory for a new request. Enforces the creation invariants in the Domain
    /// (throws <see cref="DomainException"/>) rather than trusting the caller.
    /// </summary>
    public static Request Create(
        string title,
        string? description,
        RequestType type,
        Guid requesterId,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Request title is required.");
        }

        title = title.Trim();
        if (title.Length > TitleMaxLength)
        {
            throw new DomainException($"Request title must be at most {TitleMaxLength} characters.");
        }

        if (requesterId == Guid.Empty)
        {
            throw new DomainException("Request must have a requester.");
        }

        return new Request(Guid.NewGuid(), title, description?.Trim(), type, requesterId, nowUtc);
    }
}
