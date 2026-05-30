using CompanyOps.Application.Abstractions;

namespace CompanyOps.Application.Requests.SubmitRequest;

/// <summary>
/// Handles <see cref="SubmitRequestCommand"/>: load the aggregate, call the domain
/// transition (which materializes the chain and enforces the rule), persist, return
/// the updated read model. Returns <c>null</c> when the request does not exist.
/// </summary>
public sealed class SubmitRequestHandler(
    IRequestRepository requests,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RequestDto?> HandleAsync(SubmitRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetForUpdateAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        request.Submit(command.ActorId, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RequestDto.FromDomain(request);
    }
}
