namespace UniAcademic.Infrastructure.Options;

public sealed class SeedDataOptions
{
    public const string SectionName = "SeedData";

    public bool ApplyMigrationsEnabled { get; set; }

    public bool AutoSyncEnabled { get; set; }

    public string RootPath { get; set; } = "..\\..\\seed-data";
}
