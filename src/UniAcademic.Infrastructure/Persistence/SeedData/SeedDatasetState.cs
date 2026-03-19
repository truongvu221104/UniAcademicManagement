namespace UniAcademic.Infrastructure.Persistence.SeedData;

public sealed class SeedDatasetState
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string DatasetName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string FileHash { get; set; } = string.Empty;

    public DateTime AppliedAtUtc { get; set; }

    public string Status { get; set; } = string.Empty;
}
