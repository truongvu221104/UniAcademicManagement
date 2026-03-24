using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> builder)
    {
        builder.ToTable("AttendanceSessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SessionDate)
            .HasColumnType("date");

        builder.Property(x => x.Title)
            .HasMaxLength(200);

        builder.Property(x => x.Note)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(100);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.CourseOfferingId);

        builder.HasIndex(x => new { x.CourseOfferingId, x.SessionDate, x.SessionNo })
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
