using CompanyOps.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyOps.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistence mapping for <see cref="AuditLog"/>. Append-only by design — the entity
/// has no mutators, and no update/delete path is exposed. (DB-level grants that prevent
/// even an UPDATE/DELETE by the app user are a Phase 11 concern.)
/// </summary>
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.OccurredAtUtc).IsRequired();
        builder.Property(a => a.ActorId).IsRequired();

        builder.Property(a => a.Action)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.TargetType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.TargetId).IsRequired();

        builder.Property(a => a.FromStatus).HasMaxLength(50);
        builder.Property(a => a.ToStatus).HasMaxLength(50);

        // Common query paths: "history of this request" and "recent activity".
        builder.HasIndex(a => a.TargetId);
        builder.HasIndex(a => a.OccurredAtUtc);
    }
}
