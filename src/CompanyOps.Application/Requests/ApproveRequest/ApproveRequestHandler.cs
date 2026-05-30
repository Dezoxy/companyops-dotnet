using CompanyOps.Application.Abstractions;

namespace CompanyOps.Application.Requests.ApproveRequest;

/// <summary>
/// Handles <see cref="ApproveRequestCommand"/>: load the aggregate, call the domain
/// approval (which enforces role + department scope and advances the chain), persist,
/// return the updated read model. Returns <c>null</c> when the request does not exist.
/// </summary>
public sealed class ApproveRequestHandler(
    IRequestRepository requests,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RequestDto?> HandleAsync(ApproveRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetForUpdateAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        request.Approve(command.ApproverId, command.ApproverRole, command.ApproverDepartmentId, timeProvider.GetUtcNow(), command.Note);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RequestDto.FromDomain(request);
    }
}
