using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Identity;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).HasMaxLength(150).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(150).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(100);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });

        builder.HasOne(x => x.User)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
