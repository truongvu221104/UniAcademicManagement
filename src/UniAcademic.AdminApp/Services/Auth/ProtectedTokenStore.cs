using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UniAcademic.AdminApp.Services.Auth;

public sealed class ProtectedTokenStore : ITokenStore
{
    private static readonly string TokenFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "UniAcademicManagement",
        "auth.dat");

    public async Task SaveAsync(AuthTokenSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(TokenFilePath)!);

        var json = JsonSerializer.Serialize(snapshot);
        var rawBytes = Encoding.UTF8.GetBytes(json);
        var protectedBytes = ProtectedData.Protect(rawBytes, null, DataProtectionScope.CurrentUser);

        await File.WriteAllBytesAsync(TokenFilePath, protectedBytes, cancellationToken);
    }

    public async Task<AuthTokenSnapshot?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(TokenFilePath))
        {
            return null;
        }

        var protectedBytes = await File.ReadAllBytesAsync(TokenFilePath, cancellationToken);
        var rawBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        var json = Encoding.UTF8.GetString(rawBytes);
        return JsonSerializer.Deserialize<AuthTokenSnapshot>(json);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(TokenFilePath))
        {
            File.Delete(TokenFilePath);
        }

        return Task.CompletedTask;
    }
}
