using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class CourseOfferingRosterSnapshotConfiguration : IEntityTypeConfiguration<CourseOfferingRosterSnapshot>
{
    public void Configure(EntityTypeBuilder<CourseOfferingRosterSnapshot> builder)
    {
        builder.ToTable("CourseOfferingRosterSnapshots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FinalizedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(100);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.CourseOfferingId)
            .IsUnique();

        builder.HasOne(x => x.CourseOffering)
            .WithMany()
            .HasForeignKey(x => x.CourseOfferingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
