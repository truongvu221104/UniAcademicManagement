using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class GradeResultConfiguration : IEntityTypeConfiguration<GradeResult>
{
    public void Configure(EntityTypeBuilder<GradeResult> builder)
    {
        builder.ToTable("GradeResults");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.WeightedFinalScore)
            .HasColumnType("decimal(18,4)");

        builder.Property(x => x.PassingScore)
            .HasColumnType("decimal(18,4)");

        builder.Property(x => x.CalculatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(100);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.CourseOfferingId);
        builder.HasIndex(x => x.CourseOfferingRosterSnapshotId);
        builder.HasIndex(x => new { x.CourseOfferingId, x.RosterItemId })
            .IsUnique();

        builder.HasOne(x => x.CourseOffering)
            .WithMany()
            .HasForeignKey(x => x.CourseOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CourseOfferingRosterSnapshot)
            .WithMany()
            .HasForeignKey(x => x.CourseOfferingRosterSnapshotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RosterItem)
            .WithMany()
            .HasForeignKey(x => x.RosterItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
