using CompanyOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyOps.Infrastructure;

/// <summary>
/// Applies EF Core migrations. Lives in Infrastructure so the entry point (the API's
/// one-shot `--migrate` mode / the compose migrator) doesn't reference <c>AppDbContext</c>
/// directly — migration is an Infrastructure concern.
/// </summary>
public static class DatabaseMigrationExtensions
{
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(cancellationToken);
    }
}
