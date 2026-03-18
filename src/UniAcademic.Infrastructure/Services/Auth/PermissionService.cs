using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Infrastructure.Persistence;

namespace UniAcademic.Infrastructure.Services.Auth;

public sealed class PermissionService : IPermissionService
{
    private readonly AppDbContext _dbContext;

    public PermissionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .Where(x => x.UserId == userId)
            .SelectMany(x => x.Role.RolePermissions.Select(rp => rp.Permission.Code))
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .Where(x => x.UserId == userId)
            .Select(x => x.Role.Name)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }
}
