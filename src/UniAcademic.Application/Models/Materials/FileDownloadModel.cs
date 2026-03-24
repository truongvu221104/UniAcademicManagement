namespace UniAcademic.Application.Models.Materials;

public sealed class FileDownloadModel : IAsyncDisposable
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public Stream Content { get; set; } = Stream.Null;

    public async ValueTask DisposeAsync()
    {
        if (Content is not null)
        {
            await Content.DisposeAsync();
        }
    }
}
