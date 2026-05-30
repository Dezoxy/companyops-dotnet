using CompanyOps.Application.Abstractions;
using CompanyOps.Application.Requests;

namespace CompanyOps.Application.Requests.GetRequest;

public sealed class GetRequestByIdHandler(IRequestRepository requests)
{
    public async Task<RequestDto?> HandleAsync(GetRequestByIdQuery query, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetByIdAsync(query.Id, cancellationToken);
        return request is null ? null : RequestDto.FromDomain(request);
    }
}
