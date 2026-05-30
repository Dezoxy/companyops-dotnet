using CompanyOps.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyOps.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistence mapping for <see cref="Request"/>. Keeps column types, lengths, and
/// indexes out of the Domain.
/// </summary>
internal sealed class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.ToTable("requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .HasMaxLength(Request.TitleMaxLength)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(4000);

        // Store enums as text so the database is human-readable and stable across
        // enum reordering (a recurring footgun with integer-backed enums).
        builder.Property(r => r.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.RequesterId)
            .IsRequired();

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(r => r.RequesterId);
        builder.HasIndex(r => r.Status);
    }
}
