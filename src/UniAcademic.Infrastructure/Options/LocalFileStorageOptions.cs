namespace UniAcademic.Infrastructure.Options;

public sealed class LocalFileStorageOptions
{
    public const string SectionName = "LocalFileStorage";

    public string RootPath { get; set; } = "..\\..\\storage";

    public long MaxFileSizeInBytes { get; set; } = 10 * 1024 * 1024;
}
