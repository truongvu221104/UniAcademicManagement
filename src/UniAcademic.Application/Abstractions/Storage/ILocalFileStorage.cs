namespace UniAcademic.Application.Abstractions.Storage;

public interface ILocalFileStorage
{
    long MaxFileSizeInBytes { get; }

    Task<StoredLocalFile> SaveAsync(LocalFileSaveRequest request, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default);
}

public sealed class LocalFileSaveRequest
{
    public Guid CourseOfferingId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public Stream Content { get; set; } = Stream.Null;
}

public sealed class StoredLocalFile
{
    public string RelativePath { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;
}
