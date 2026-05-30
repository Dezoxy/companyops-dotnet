using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Requests;

namespace CompanyOps.Application.Requests.CreateRequest;

/// <summary>
/// Handles <see cref="CreateRequestCommand"/>: build the aggregate (Domain enforces
/// its invariants), persist it, and return the read model.
/// </summary>
/// <remarks>
/// Plain injectable handler — no mediator yet. A mediator (with validation/logging/
/// audit pipeline behaviours) is introduced later, when those cross-cutting concerns
/// earn the dependency. <see cref="TimeProvider"/> is injected so "now" is testable.
/// </remarks>
public sealed class CreateRequestHandler(
    IRequestRepository requests,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RequestDto> HandleAsync(CreateRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = Request.Create(
            command.Title,
            command.Description,
            command.Type,
            command.RequesterId,
            command.DepartmentId,
            timeProvider.GetUtcNow());

        requests.Add(request);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RequestDto.FromDomain(request);
    }
}
