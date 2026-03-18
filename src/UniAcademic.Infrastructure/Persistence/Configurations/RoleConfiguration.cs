using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Identity;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(250).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.NormalizedName).IsUnique();
    }
}
