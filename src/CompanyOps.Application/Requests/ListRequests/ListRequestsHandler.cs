using CompanyOps.Application.Abstractions;
using CompanyOps.Application.Common;
using CompanyOps.Application.Requests;

namespace CompanyOps.Application.Requests.ListRequests;

public sealed class ListRequestsHandler(IRequestRepository requests)
{
    public async Task<PagedResult<RequestDto>> HandleAsync(ListRequestsQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page ?? new PageRequest();
        var items = await requests.ListAsync(query.RequesterId, query.DepartmentId, page.Skip, page.Take, cancellationToken);
        var total = await requests.CountAsync(query.RequesterId, query.DepartmentId, cancellationToken);
        var dtos = items.Select(RequestDto.FromDomain).ToList();
        return new PagedResult<RequestDto>(dtos, total, page.Page, page.PageSize);
    }
}
