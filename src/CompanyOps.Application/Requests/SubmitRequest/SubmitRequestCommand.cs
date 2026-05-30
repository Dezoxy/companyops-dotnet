namespace CompanyOps.Application.Requests.SubmitRequest;

/// <summary>
/// Use-case input for submitting a draft request for approval. Materializes the
/// configured approval chain for the request's type (the domain does this).
/// <paramref name="ActorId"/> is the authenticated submitter; the domain enforces that
/// only the requester may submit their own request.
/// </summary>
public sealed record SubmitRequestCommand(Guid RequestId, Guid ActorId);
