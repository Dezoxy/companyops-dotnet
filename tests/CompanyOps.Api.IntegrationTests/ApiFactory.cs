using System.Net.Http.Json;
using CompanyOps.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// Boots the real API against throwaway Postgres 18 and Keycloak 26 containers, with
/// the committed realm imported, so auth is exercised end-to-end with real JWTs.
/// Both the token fetch and the API talk to Keycloak on the same host:port, so the
/// token issuer matches the API's configured authority.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("companyops")
        .WithUsername("companyops")
        .WithPassword("localdev_only_not_a_secret")
        .Build();

    private readonly IContainer _keycloak = new ContainerBuilder("quay.io/keycloak/keycloak:26.0")
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin")
        .WithResourceMapping(new FileInfo(FindRealmFile()), "/opt/keycloak/data/import/")
        .WithCommand("start-dev", "--import-realm")
        .WithPortBinding(8080, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r =>
            r.ForPort(8080).ForPath("/realms/companyops/.well-known/openid-configuration")))
        .Build();

    private string KeycloakBaseUrl => $"http://localhost:{_keycloak.GetMappedPublicPort(8080)}";

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _keycloak.StartAsync());

        // First Services access builds the host with the container-derived settings below;
        // apply the schema before any test runs.
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development"); // RequireHttpsMetadata=false against the http Keycloak
        builder.UseSetting("ConnectionStrings:CompanyOps", _postgres.GetConnectionString());
        builder.UseSetting("Keycloak:Authority", $"{KeycloakBaseUrl}/realms/companyops");
        builder.UseSetting("Keycloak:Audience", "companyops-api");
    }

    /// <summary>Fetch a real access token for a seed user via the realm's password grant.</summary>
    public async Task<string> GetTokenAsync(string username, string password = "Passw0rd!")
    {
        using var http = new HttpClient();
        var response = await http.PostAsync(
            $"{KeycloakBaseUrl}/realms/companyops/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "companyops-api",
                ["username"] = username,
                ["password"] = password,
            }));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return payload!.AccessToken;
    }

    public HttpClient CreateClientWithToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    public new async Task DisposeAsync()
    {
        await _keycloak.DisposeAsync();
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    // Walk up from the test output dir to the repo root to locate the committed realm export.
    private static string FindRealmFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "infra", "keycloak", "realm-companyops.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("Could not locate infra/keycloak/realm-companyops.json from the test output directory.");
    }

    private sealed record TokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken);
}
