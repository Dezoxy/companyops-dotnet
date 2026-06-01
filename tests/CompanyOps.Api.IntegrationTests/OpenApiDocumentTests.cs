using System.Net;
using System.Text.Json;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// The runtime dev OpenAPI document (<c>/openapi/v1.json</c>, mapped in Development) must document
/// the Bearer security scheme but must NOT advertise the production server — otherwise local Scalar
/// or a client generated from the localhost document would send requests to production. The
/// production server belongs only to the build-time/canonical contract (gated in
/// <c>BearerSecuritySchemeTransformer</c> via <c>BuildTimeOpenApi.IsGenerating</c>).
/// </summary>
[Collection("Integration")]
public sealed class OpenApiDocumentTests(ApiFactory factory)
{
    [Fact]
    public async Task RuntimeDocument_DocumentsBearer_ButDoesNotAdvertiseProductionServer()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        // The runtime doc still documents auth (dev Scalar should prompt for a token).
        Assert.True(root.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));

        // ...but never advertises the production target.
        Assert.DoesNotContain("toomhorvath.com", root.GetRawText());
    }
}
