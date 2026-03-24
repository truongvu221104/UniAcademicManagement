using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class GradeEntryConfiguration : IEntityTypeConfiguration<GradeEntry>
{
    public void Configure(EntityTypeBuilder<GradeEntry> builder)
    {
        builder.ToTable("GradeEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Score)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Note)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(100);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.RosterItemId);

        builder.HasIndex(x => new { x.GradeCategoryId, x.RosterItemId })
            .IsUnique();

        builder.HasOne(x => x.GradeCategory)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.GradeCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RosterItem)
            .WithMany()
            .HasForeignKey(x => x.RosterItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
