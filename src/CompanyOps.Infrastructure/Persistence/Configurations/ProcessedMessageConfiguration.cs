using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyOps.Infrastructure.Persistence.Configurations;

internal sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.ProcessedAtUtc).IsRequired();

        // The table only needs to retain ids for as long as the broker can redeliver.
        // A periodic purge of rows older than the redelivery window is a Phase 10/11
        // operational concern (an index on ProcessedAtUtc would support that query).
    }
}
