using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Note)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(100);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => new { x.StudentProfileId, x.CourseOfferingId })
            .IsUnique();

        builder.HasIndex(x => x.CourseOfferingId);

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => new { x.CourseOfferingId, x.Status });

        builder.HasIndex(x => new { x.StudentProfileId, x.Status });

        builder.HasOne(x => x.StudentProfile)
            .WithMany()
            .HasForeignKey(x => x.StudentProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CourseOffering)
            .WithMany()
            .HasForeignKey(x => x.CourseOfferingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
