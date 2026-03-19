using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniAcademic.Infrastructure.Persistence.SeedData;

namespace UniAcademic.Infrastructure.Persistence.Configurations;

public sealed class SeedDatasetStateConfiguration : IEntityTypeConfiguration<SeedDatasetState>
{
    public void Configure(EntityTypeBuilder<SeedDatasetState> builder)
    {
        builder.ToTable("SeedDatasetStates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DatasetName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.FilePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.FileHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.DatasetName)
            .IsUnique();
    }
}
