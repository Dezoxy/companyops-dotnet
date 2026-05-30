using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// CORS for the SPA (Phase 13): the configured SPA origin is allowed; others are not. The test
/// API runs in Development, so `Cors:AllowedOrigins` is `http://localhost:4200`.
/// </summary>
[Collection("Integration")]
public sealed class CorsTests(ApiFactory factory)
{
    [Fact]
    public async Task Preflight_FromTheSpaOrigin_IsAllowed()
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/requests");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.Contains("http://localhost:4200", response.Headers.GetValues("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Preflight_FromAnUnknownOrigin_IsNotAllowed()
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/requests");
        request.Headers.Add("Origin", "https://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
