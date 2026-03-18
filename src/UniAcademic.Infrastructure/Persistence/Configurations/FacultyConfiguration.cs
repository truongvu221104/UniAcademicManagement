using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class FacultyConfiguration : IEntityTypeConfiguration<Faculty>
{
    public void Configure(EntityTypeBuilder<Faculty> builder)
    {
        builder.ToTable("Faculties");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(100);

        builder.Property(x => x.DeletedBy)
            .HasMaxLength(100);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.HasIndex(x => x.Status);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
