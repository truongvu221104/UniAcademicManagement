namespace UniAcademic.AdminApp.Services.Auth;

public interface ITokenStore
{
    Task SaveAsync(AuthTokenSnapshot snapshot, CancellationToken cancellationToken = default);

    Task<AuthTokenSnapshot?> LoadAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
