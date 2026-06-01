using CompanyOps.Application.Abstractions;
using CompanyOps.Application.Common;
using CompanyOps.Application.Requests;

namespace CompanyOps.Application.Requests.ListRequests;

public sealed class ListRequestsHandler(IRequestRepository requests)
{
    public async Task<IReadOnlyList<RequestDto>> HandleAsync(ListRequestsQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page ?? new PageRequest();
        var all = await requests.ListAsync(query.RequesterId, query.DepartmentId, page.Skip, page.Take, cancellationToken);
        return all.Select(RequestDto.FromDomain).ToList();
    }
}
