using Xunit;

namespace CompanyOps.Api.IntegrationTests;

/// <summary>
/// Shares one <see cref="ApiFactory"/> (one set of Postgres/Keycloak/RabbitMQ containers)
/// across all integration test classes, and runs them serially. Tests scope their
/// assertions to their own request ids so they don't interfere through shared state.
/// </summary>
[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<ApiFactory>;
