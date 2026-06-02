using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// Rate limiting (enterprise-optional follow-up). Exercised in isolation: a derived host with a
/// tiny per-caller limit and its <em>own</em> limiter state — so firing past the limit as one user
/// returns 429 without touching the shared suite's rate buckets (which run with a high limit).
/// </summary>
[Collection("Integration")]
public sealed class RateLimitingTests(ApiFactory factory)
{
    [Fact]
    public async Task ExceedingThePerUserLimit_Returns429()
    {
        const int limit = 3;
        await using var limited = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimiting:PermitLimit", limit.ToString()));

        var token = await factory.GetTokenAsync("employee.eng");
        using var client = limited.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < limit + 1; i++)
        {
            responses.Add(await client.GetAsync("/requests"));
        }

        Assert.Equal(limit, responses.Count(r => r.StatusCode == HttpStatusCode.OK)); // the first `limit` are allowed
        var rejected = responses.First(r => r.StatusCode == HttpStatusCode.TooManyRequests); // the one over the limit
        // The 429 carries Retry-After and an RFC 7807 problem+json body — matching the contract.
        Assert.NotNull(rejected.Headers.RetryAfter);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
    }
}
