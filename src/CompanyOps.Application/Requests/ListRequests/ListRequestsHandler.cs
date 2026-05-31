using CompanyOps.Application.Abstractions;
using CompanyOps.Application.Requests;

namespace CompanyOps.Application.Requests.ListRequests;

public sealed class ListRequestsHandler(IRequestRepository requests)
{
    public async Task<IReadOnlyList<RequestDto>> HandleAsync(ListRequestsQuery query, CancellationToken cancellationToken = default)
    {
        var all = await requests.ListAsync(query.RequesterId, query.DepartmentId, cancellationToken);
        return all.Select(RequestDto.FromDomain).ToList();
    }
}
