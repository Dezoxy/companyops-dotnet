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

        builder.Property(r => r.Priority)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Nullable: only helpdesk requests carry a category (enforced in the Domain).
        builder.Property(r => r.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.RequesterId)
            .IsRequired();

        builder.Property(r => r.DepartmentId)
            .IsRequired();

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(r => r.RequesterId);
        builder.HasIndex(r => r.DepartmentId);
        builder.HasIndex(r => r.Status);

        // ApprovalStep is owned by the Request aggregate (ADR 0006): its own table,
        // no DbSet, loaded with the request. Map the private list field, not a setter.
        builder.OwnsMany(r => r.ApprovalSteps, step =>
        {
            step.ToTable("approval_steps");
            step.WithOwner().HasForeignKey("RequestId");
            step.HasKey(s => s.Id);

            // The aggregate assigns the step Id (ApprovalStep ctor), so EF must treat a
            // new step reachable from a tracked Request as Added (INSERT), not Modified.
            // Without this, EF assumes the Guid key is store-generated and issues an
            // UPDATE that affects 0 rows → DbUpdateConcurrencyException on submit.
            step.Property(s => s.Id).ValueGeneratedNever();

            step.Property(s => s.Order).IsRequired();
            step.Property(s => s.IsRequired).IsRequired();

            step.Property(s => s.RequiredRole)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            step.Property(s => s.Scope)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            step.Property(s => s.Decision)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            step.Property(s => s.Note).HasMaxLength(1000);

            // Stable ordering of the chain when materialized back from the database.
            step.HasIndex("RequestId", nameof(ApprovalStep.Order)).IsUnique();
        });

        builder.Navigation(r => r.ApprovalSteps)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
