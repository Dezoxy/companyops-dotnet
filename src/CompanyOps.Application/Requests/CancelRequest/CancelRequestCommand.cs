namespace CompanyOps.Application.Requests.CancelRequest;

/// <summary>
/// Use-case input for cancelling a request. <paramref name="ActorId"/> comes from the
/// authenticated principal; the Domain enforces that only the requester may cancel, and only from
/// Draft or Submitted (before approval completes).
/// </summary>
public sealed record CancelRequestCommand(Guid RequestId, Guid ActorId);
