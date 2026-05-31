using CompanyOps.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyOps.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistence mapping for <see cref="Comment"/>. A standalone table keyed by request id —
/// no enforced FK / navigation, keeping it decoupled from the Request aggregate (the handler
/// checks the request exists). Indexed by request for thread lookups.
/// </summary>
internal sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.RequestId).IsRequired();
        builder.Property(c => c.AuthorId).IsRequired();

        builder.Property(c => c.Body)
            .HasMaxLength(Comment.BodyMaxLength)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.HasIndex(c => c.RequestId);
    }
}
