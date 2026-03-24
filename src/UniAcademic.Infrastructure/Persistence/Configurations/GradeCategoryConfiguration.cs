using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class GradeCategoryConfiguration : IEntityTypeConfiguration<GradeCategory>
{
    public void Configure(EntityTypeBuilder<GradeCategory> builder)
    {
        builder.ToTable("GradeCategories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Weight)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MaxScore)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(100);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.CourseOfferingId);

        builder.HasIndex(x => x.CourseOfferingRosterSnapshotId);

        builder.HasIndex(x => new { x.CourseOfferingId, x.Name })
            .IsUnique();

        builder.HasOne(x => x.CourseOffering)
            .WithMany()
            .HasForeignKey(x => x.CourseOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CourseOfferingRosterSnapshot)
            .WithMany()
            .HasForeignKey(x => x.CourseOfferingRosterSnapshotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
