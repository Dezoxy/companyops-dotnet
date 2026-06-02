using CompanyOps.Application.Common;
using CompanyOps.Application.Requests.ListRequests;
using CompanyOps.Domain.Requests;
using Xunit;

namespace CompanyOps.Application.Tests;

/// <summary>
/// The list handler returns a page of requests plus the unpaged total in the same scope, so the
/// client can render the pagination footer. Fakes, no DB.
/// </summary>
public class ListRequestsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Requester = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Department = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly FakeRequestRepository _requests = new();

    private void Seed(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _requests.Seed(Request.Create(
                $"Request {i}", null, RequestType.Procurement, RequestPriority.Medium, null,
                Requester, Department, Now.AddMinutes(i)));
        }
    }

    [Fact]
    public async Task HandleAsync_ReturnsRequestedPage_WithUnpagedTotal()
    {
        Seed(12);
        var handler = new ListRequestsHandler(_requests);

        var result = await handler.HandleAsync(
            new ListRequestsQuery(Page: new PageRequest(page: 2, pageSize: 5)));

        Assert.Equal(5, result.Items.Count); // the page
        Assert.Equal(12, result.Total);      // total across all pages, not just the page
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(3, result.TotalPages);  // ceil(12 / 5)
    }

    [Fact]
    public async Task HandleAsync_LastPartialPage_ReturnsRemainder()
    {
        Seed(12);
        var handler = new ListRequestsHandler(_requests);

        var result = await handler.HandleAsync(
            new ListRequestsQuery(Page: new PageRequest(page: 3, pageSize: 5)));

        Assert.Equal(2, result.Items.Count); // 12 - 10
        Assert.Equal(12, result.Total);
    }

    [Fact]
    public async Task HandleAsync_PastTheEnd_ReturnsEmptyPageButRealTotal()
    {
        Seed(3);
        var handler = new ListRequestsHandler(_requests);

        var result = await handler.HandleAsync(
            new ListRequestsQuery(Page: new PageRequest(page: 99, pageSize: 5)));

        Assert.Empty(result.Items);
        Assert.Equal(3, result.Total); // footer still shows the true total
    }
}
