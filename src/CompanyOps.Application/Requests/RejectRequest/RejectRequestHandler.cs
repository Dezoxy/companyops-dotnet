using CompanyOps.Application.Abstractions;

namespace CompanyOps.Application.Requests.RejectRequest;

/// <summary>
/// Handles <see cref="RejectRequestCommand"/>: load the aggregate, call the domain
/// rejection (which enforces eligibility and moves the request to Rejected), persist,
/// return the updated read model. Returns <c>null</c> when the request does not exist.
/// </summary>
public sealed class RejectRequestHandler(
    IRequestRepository requests,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RequestDto?> HandleAsync(RejectRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetForUpdateAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        request.Reject(command.ApproverId, command.ApproverRole, command.ApproverDepartmentId, timeProvider.GetUtcNow(), command.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RequestDto.FromDomain(request);
    }
}
