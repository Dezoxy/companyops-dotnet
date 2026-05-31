using CompanyOps.Domain.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyOps.Infrastructure.Persistence.Configurations;

/// <summary>Persistence mapping for <see cref="Asset"/>. Enums stored as text (like Request),
/// tag uniquely indexed.</summary>
internal sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("assets");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Tag).HasMaxLength(Asset.TagMaxLength).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(Asset.NameMaxLength).IsRequired();

        builder.Property(a => a.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.CreatedAtUtc).IsRequired();

        builder.HasIndex(a => a.Tag).IsUnique();
    }
}
