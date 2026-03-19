using System.Text.Json;

namespace UniAcademic.Infrastructure.SeedData.Services;

public sealed class JsonSeedDataFileReader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyCollection<T>> ReadListAsync<T>(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        var data = await JsonSerializer.DeserializeAsync<List<T>>(stream, SerializerOptions, cancellationToken);
        return data ?? [];
    }
}
