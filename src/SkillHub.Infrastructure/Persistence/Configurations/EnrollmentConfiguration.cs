using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillHub.Domain.Entities;

namespace SkillHub.Infrastructure.Persistence.Configurations;

public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.EnrolledAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CancelledAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CancelledBy)
            .HasMaxLength(256);

        // Partial unique index: guarantees at most ONE Active enrollment per (ParticipantId, WorkshopId)
        // while preserving Cancelled historical records and allowing future re-enrollment.
        builder.HasIndex(x => new { x.ParticipantId, x.WorkshopId })
            .HasDatabaseName("IX_enrollments_ParticipantId_WorkshopId_Active")
            .HasFilter("\"Status\" = 'Active'")
            .IsUnique();

        builder.HasIndex(x => new { x.WorkshopId, x.Status });
        builder.HasIndex(x => new { x.ParticipantId, x.Status });

        builder.HasOne(x => x.Participant)
            .WithMany(p => p.Enrollments)
            .HasForeignKey(x => x.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Workshop)
            .WithMany(w => w.Enrollments)
            .HasForeignKey(x => x.WorkshopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(x => x.DomainEvents);
    }
}
