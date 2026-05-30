using CompanyOps.Application.Abstractions;

namespace CompanyOps.Application.Requests.FulfillRequest;

/// <summary>
/// Handles <see cref="FulfillRequestCommand"/>: load the aggregate, call the domain
/// fulfillment (Approved → Completed), persist, return the updated read model. Returns
/// <c>null</c> when the request does not exist.
/// </summary>
public sealed class FulfillRequestHandler(
    IRequestRepository requests,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RequestDto?> HandleAsync(FulfillRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetForUpdateAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        request.Fulfill(command.ActorId, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RequestDto.FromDomain(request);
    }
}
