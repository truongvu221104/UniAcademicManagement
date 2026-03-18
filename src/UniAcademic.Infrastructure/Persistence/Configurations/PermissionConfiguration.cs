using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Identity;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Module).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(250).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
