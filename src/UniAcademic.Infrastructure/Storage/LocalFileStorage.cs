using Microsoft.Extensions.Options;
using UniAcademic.Application.Abstractions.Storage;
using UniAcademic.Infrastructure.Options;

namespace UniAcademic.Infrastructure.Storage;

public sealed class LocalFileStorage : ILocalFileStorage
{
    private readonly LocalFileStorageOptions _options;
    private readonly string _rootPath;

    public LocalFileStorage(IOptions<LocalFileStorageOptions> options)
    {
        _options = options.Value;
        _rootPath = ResolveRootPath(_options.RootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public long MaxFileSizeInBytes => _options.MaxFileSizeInBytes;

    public async Task<StoredLocalFile> SaveAsync(LocalFileSaveRequest request, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(request.OriginalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine("course-materials", request.CourseOfferingId.ToString(), storedFileName)
            .Replace('\\', '/');
        var fullPath = ResolveFullPath(relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        request.Content.Position = 0;
        await using var output = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await request.Content.CopyToAsync(output, cancellationToken);

        return new StoredLocalFile
        {
            RelativePath = relativePath,
            StoredFileName = storedFileName
        };
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveFullPath(relativePath);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveFullPath(relativePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    private static string ResolveRootPath(string configuredRootPath)
    {
        return Path.GetFullPath(Path.IsPathRooted(configuredRootPath)
            ? configuredRootPath
            : Path.Combine(Directory.GetCurrentDirectory(), configuredRootPath));
    }

    private string ResolveFullPath(string relativePath)
    {
        var normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_rootPath, normalizedRelativePath);
    }
}
