using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class CoursePrerequisiteConfiguration : IEntityTypeConfiguration<CoursePrerequisite>
{
    public void Configure(EntityTypeBuilder<CoursePrerequisite> builder)
    {
        builder.ToTable("CoursePrerequisites");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(100);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.CourseId);

        builder.HasIndex(x => x.PrerequisiteCourseId);

        builder.HasIndex(x => new { x.CourseId, x.PrerequisiteCourseId })
            .IsUnique();

        builder.HasOne(x => x.Course)
            .WithMany(x => x.Prerequisites)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PrerequisiteCourse)
            .WithMany(x => x.RequiredForCourses)
            .HasForeignKey(x => x.PrerequisiteCourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
