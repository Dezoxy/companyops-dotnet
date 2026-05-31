using CompanyOps.Application.Abstractions;
using CompanyOps.Domain.Assets;
using CompanyOps.Domain.Auditing;
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
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Asset> Assets => Set<Asset>();

    // OutboxMessage is internal infrastructure (ADR 0007) — no public DbSet; the model
    // registers it via OutboxMessageConfiguration, and the publisher/relay use Set<T>().

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration<T> in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
