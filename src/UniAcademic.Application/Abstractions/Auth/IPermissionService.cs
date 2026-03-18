namespace UniAcademic.Application.Abstractions.Auth;

public interface IPermissionService
{
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default);
}
