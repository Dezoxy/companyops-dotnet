using System.Net;
using CompanyOps.Api.Auth;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// Unit tests for the API's audit-source provenance: it reports the current request's remote IP,
/// or null when there is no request (the Worker). TestServer provides no RemoteIpAddress, so the
/// end-to-end populated IP is exercised behind the deployed ForwardedHeaders, not in the suite —
/// here we pin the capture logic directly. No host/container.
/// </summary>
public sealed class HttpAuditContextTests
{
    [Fact]
    public void SourceIp_ReturnsTheConnectionRemoteIp()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.9");
        var context = new HttpAuditContext(new HttpContextAccessor { HttpContext = httpContext });

        Assert.Equal("203.0.113.9", context.SourceIp);
    }

    [Fact]
    public void SourceIp_IsNull_WhenThereIsNoHttpContext()
    {
        var context = new HttpAuditContext(new HttpContextAccessor()); // no current request — e.g. the Worker

        Assert.Null(context.SourceIp);
    }
}
