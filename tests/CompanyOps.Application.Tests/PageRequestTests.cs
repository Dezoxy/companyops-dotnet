using CompanyOps.Application.Common;
using Xunit;

namespace CompanyOps.Application.Tests;

/// <summary>
/// The paging window: defaults, clamping, and — critically — that a huge page can't overflow
/// <see cref="PageRequest.Skip"/> into a negative offset (which would 500 against EF/Postgres).
/// </summary>
public class PageRequestTests
{
    [Fact]
    public void Defaults_AreFirstPageAndDefaultSize()
    {
        var p = new PageRequest();
        Assert.Equal(1, p.Page);
        Assert.Equal(PageRequest.DefaultPageSize, p.PageSize);
        Assert.Equal(0, p.Skip);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void Page_BelowOne_IsClampedToOne(int input, int expected)
        => Assert.Equal(expected, new PageRequest(input).Page);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10_000, PageRequest.MaxPageSize)]
    [InlineData(25, 25)]
    public void PageSize_IsClampedToRange(int input, int expected)
        => Assert.Equal(expected, new PageRequest(pageSize: input).PageSize);

    [Fact]
    public void HugePage_DoesNotOverflowSkip()
    {
        // (Page-1)*PageSize would overflow int; Skip must stay non-negative (saturated).
        var p = new PageRequest(page: int.MaxValue, pageSize: PageRequest.MaxPageSize);
        Assert.True(p.Skip >= 0);
        Assert.Equal(int.MaxValue, p.Skip);
    }
}
