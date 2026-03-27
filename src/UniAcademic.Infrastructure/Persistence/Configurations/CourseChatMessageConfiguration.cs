using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class CourseChatMessageConfiguration : IEntityTypeConfiguration<CourseChatMessage>
{
    public void Configure(EntityTypeBuilder<CourseChatMessage> builder)
    {
        builder.ToTable("CourseChatMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SenderUsername)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.SenderDisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.SenderRole)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.MessageText)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(100);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.CourseOfferingId);

        builder.HasIndex(x => x.SenderUserId);

        builder.HasIndex(x => new { x.CourseOfferingId, x.CreatedAtUtc });

        builder.HasOne(x => x.CourseOffering)
            .WithMany()
            .HasForeignKey(x => x.CourseOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SenderUser)
            .WithMany()
            .HasForeignKey(x => x.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
