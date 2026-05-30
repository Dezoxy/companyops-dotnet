using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyOps.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Payload).IsRequired(); // JSON; unbounded text
        builder.Property(m => m.OccurredAtUtc).IsRequired();
        builder.Property(m => m.Error).HasMaxLength(2000);

        // The relay scans for unprocessed rows oldest-first; index that access path.
        builder.HasIndex(m => m.ProcessedAtUtc);
    }
}
