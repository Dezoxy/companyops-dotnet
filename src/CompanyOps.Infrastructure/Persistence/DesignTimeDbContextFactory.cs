using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CompanyOps.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> at design time (migrations, scripts). It removes
/// the dependency on the API host's DI/config so migrations work without a running
/// app or full configuration. The connection string here only needs to be valid
/// enough to build the model; it can be overridden via <c>COMPANYOPS_DB</c>.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("COMPANYOPS_DB")
            ?? "Host=localhost;Port=5432;Database=companyops;Username=companyops;Password=localdev_only_not_a_secret";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
