using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class CourseMaterialConfiguration : IEntityTypeConfiguration<CourseMaterial>
{
    public void Configure(EntityTypeBuilder<CourseMaterial> builder)
    {
        builder.ToTable("CourseMaterials");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(100);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.CourseOfferingId);

        builder.HasIndex(x => x.FileMetadataId)
            .IsUnique();

        builder.HasIndex(x => new { x.CourseOfferingId, x.SortOrder });

        builder.HasOne(x => x.CourseOffering)
            .WithMany()
            .HasForeignKey(x => x.CourseOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FileMetadata)
            .WithOne(x => x.CourseMaterial)
            .HasForeignKey<CourseMaterial>(x => x.FileMetadataId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
