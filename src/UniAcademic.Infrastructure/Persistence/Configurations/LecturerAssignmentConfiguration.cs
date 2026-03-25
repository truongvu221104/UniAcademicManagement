using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class LecturerAssignmentConfiguration : IEntityTypeConfiguration<LecturerAssignment>
{
    public void Configure(EntityTypeBuilder<LecturerAssignment> builder)
    {
        builder.ToTable("LecturerAssignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(100);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => new { x.CourseOfferingId, x.LecturerProfileId })
            .IsUnique();

        builder.HasIndex(x => new { x.CourseOfferingId, x.IsPrimary });

        builder.HasIndex(x => x.LecturerProfileId);

        builder.HasOne(x => x.CourseOffering)
            .WithMany()
            .HasForeignKey(x => x.CourseOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LecturerProfile)
            .WithMany()
            .HasForeignKey(x => x.LecturerProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
