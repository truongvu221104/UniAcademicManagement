using System.Security.Cryptography;
using System.Text;

namespace UniAcademic.Infrastructure.SeedData.Services;

public sealed class DatasetHashService
{
    public async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        return ComputeHash(content);
    }

    public string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
