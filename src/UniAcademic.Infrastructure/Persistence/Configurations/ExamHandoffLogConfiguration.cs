using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class ExamHandoffLogConfiguration : IEntityTypeConfiguration<ExamHandoffLog>
{
    public void Configure(EntityTypeBuilder<ExamHandoffLog> builder)
    {
        builder.ToTable("ExamHandoffLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(100);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.CourseOfferingId);
        builder.HasIndex(x => x.RosterSnapshotId);
        builder.HasIndex(x => new { x.CourseOfferingId, x.SentAtUtc });

        builder.HasOne(x => x.CourseOffering)
            .WithMany()
            .HasForeignKey(x => x.CourseOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RosterSnapshot)
            .WithMany()
            .HasForeignKey(x => x.RosterSnapshotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
