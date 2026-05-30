using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace CompanyOps.Infrastructure.Persistence;

/// <summary>
/// EF Core context. Implements <see cref="IUnitOfWork"/> so the Application layer
/// commits through a port, never against the concrete context.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Request> Requests => Set<Request>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration<T> in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
