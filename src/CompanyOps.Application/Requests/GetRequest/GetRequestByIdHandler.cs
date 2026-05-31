using CompanyOps.Application.Abstractions;
using CompanyOps.Application.Requests;

namespace CompanyOps.Application.Requests.GetRequest;

public sealed class GetRequestByIdHandler(IRequestRepository requests)
{
    public async Task<RequestDto?> HandleAsync(GetRequestByIdQuery query, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetByIdAsync(query.Id, cancellationToken);
        if (request is null)
        {
            return null;
        }

        // Out-of-scope reads return not-found (the caller gets a 404), so a request's existence
        // isn't revealed to someone not entitled to see it (RequestReadScope is shared with the
        // comment thread). The Api sets at most one filter, from the caller's role.
        if (RequestReadScope.IsOutOfScope(request, query.RequesterId, query.DepartmentId))
        {
            return null;
        }

        return RequestDto.FromDomain(request);
    }
}
