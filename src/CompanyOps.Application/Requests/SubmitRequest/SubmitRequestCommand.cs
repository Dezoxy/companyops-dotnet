namespace CompanyOps.Application.Requests.SubmitRequest;

/// <summary>
/// Use-case input for submitting a draft request for approval. Materializes the
/// configured approval chain for the request's type (the domain does this).
/// </summary>
public sealed record SubmitRequestCommand(Guid RequestId);
