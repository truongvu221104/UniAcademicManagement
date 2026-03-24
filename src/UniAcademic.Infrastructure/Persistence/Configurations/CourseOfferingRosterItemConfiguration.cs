using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class CourseOfferingRosterItemConfiguration : IEntityTypeConfiguration<CourseOfferingRosterItem>
{
    public void Configure(EntityTypeBuilder<CourseOfferingRosterItem> builder)
    {
        builder.ToTable("CourseOfferingRosterItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StudentCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.StudentFullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.StudentClassName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CourseOfferingCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CourseCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CourseName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.SemesterName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(100);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.RosterSnapshotId);

        builder.HasIndex(x => x.EnrollmentId);

        builder.HasIndex(x => x.StudentProfileId);

        builder.HasOne(x => x.RosterSnapshot)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.RosterSnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Enrollment>()
            .WithMany()
            .HasForeignKey(x => x.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StudentProfile>()
            .WithMany()
            .HasForeignKey(x => x.StudentProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
