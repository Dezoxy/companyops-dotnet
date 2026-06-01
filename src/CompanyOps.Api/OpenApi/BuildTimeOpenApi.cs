using System.Reflection;

namespace CompanyOps.Api.OpenApi;

/// <summary>
/// True when the process is the build-time OpenAPI generator
/// (<c>Microsoft.Extensions.ApiDescription.Server</c> / <c>dotnet getdocument</c>), as opposed to
/// the running API. Used to (a) supply placeholder config so the host constructs without real
/// infrastructure, and (b) emit the production <c>servers</c> entry only into the canonical
/// generated contract — never into the runtime dev document, which must stay relative to localhost.
/// </summary>
internal static class BuildTimeOpenApi
{
    public static bool IsGenerating =>
        Assembly.GetEntryAssembly()?.GetName().Name is "dotnet-getdocument" or "GetDocument.Insider";
}
