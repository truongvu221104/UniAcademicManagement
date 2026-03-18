using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Identity;
using UniAcademic.Infrastructure.Options;
using UniAcademic.Infrastructure.Persistence;

namespace UniAcademic.Infrastructure.Seed;

public sealed class AuthSeedData
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly BootstrapAdminOptions _bootstrapAdminOptions;

    public AuthSeedData(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IOptions<BootstrapAdminOptions> bootstrapAdminOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _bootstrapAdminOptions = bootstrapAdminOptions.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken);
        var superAdminRole = await SeedSuperAdminRoleAsync(cancellationToken);
        await SeedAdminUserAsync(superAdminRole, cancellationToken);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Permissions.Select(x => x.Code).ToListAsync(cancellationToken);
        var missingPermissions = PermissionConstants.All
            .Where(permission => !existing.Contains(permission, StringComparer.OrdinalIgnoreCase))
            .Select(permission =>
            {
                var parts = permission.Split('.', StringSplitOptions.RemoveEmptyEntries);
                var module = parts.Length >= 2 ? string.Join('.', parts[..2]) : permission;
                var action = parts.Length > 0 ? parts[^1] : permission;

                return new Permission
                {
                    Code = permission,
                    Module = module,
                    Action = action,
                    Description = permission,
                    CreatedBy = "seed"
                };
            })
            .ToList();

        if (missingPermissions.Count == 0)
        {
            return;
        }

        await _dbContext.Permissions.AddRangeAsync(missingPermissions, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Role> SeedSuperAdminRoleAsync(CancellationToken cancellationToken)
    {
        var normalizedName = "SUPERADMIN";
        var existingRole = await _dbContext.Roles
            .Include(x => x.RolePermissions)
            .FirstOrDefaultAsync(x => x.NormalizedName == normalizedName, cancellationToken);

        if (existingRole is not null)
        {
            await EnsureRolePermissionsAsync(existingRole, cancellationToken);
            return existingRole;
        }

        var role = new Role
        {
            Name = "SuperAdmin",
            NormalizedName = normalizedName,
            Description = "Bootstrap system administrator role",
            CreatedBy = "seed"
        };

        await _dbContext.Roles.AddAsync(role, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await EnsureRolePermissionsAsync(role, cancellationToken);

        return role;
    }

    private async Task EnsureRolePermissionsAsync(Role role, CancellationToken cancellationToken)
    {
        var permissionIds = await _dbContext.Permissions.Select(x => x.Id).ToListAsync(cancellationToken);
        var existingPermissionIds = await _dbContext.RolePermissions
            .Where(x => x.RoleId == role.Id)
            .Select(x => x.PermissionId)
            .ToListAsync(cancellationToken);

        var missing = permissionIds
            .Except(existingPermissionIds)
            .Select(permissionId => new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
            })
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        await _dbContext.RolePermissions.AddRangeAsync(missing, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(Role superAdminRole, CancellationToken cancellationToken)
    {
        var normalizedUsername = _bootstrapAdminOptions.Username.Trim().ToUpperInvariant();
        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(x => x.NormalizedUsername == normalizedUsername, cancellationToken);

        if (existingUser is not null)
        {
            var hasRole = await _dbContext.UserRoles.AnyAsync(x => x.UserId == existingUser.Id && x.RoleId == superAdminRole.Id, cancellationToken);
            if (!hasRole)
            {
                await _dbContext.UserRoles.AddAsync(new UserRole
                {
                    UserId = existingUser.Id,
                    RoleId = superAdminRole.Id
                }, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var user = new User
        {
            Username = _bootstrapAdminOptions.Username.Trim(),
            NormalizedUsername = normalizedUsername,
            Email = _bootstrapAdminOptions.Email.Trim(),
            NormalizedEmail = _bootstrapAdminOptions.Email.Trim().ToUpperInvariant(),
            DisplayName = _bootstrapAdminOptions.DisplayName.Trim(),
            PasswordHash = _passwordHasher.HashPassword(_bootstrapAdminOptions.Password),
            IsActive = true,
            CreatedBy = "seed"
        };

        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.UserRoles.AddAsync(new UserRole
        {
            UserId = user.Id,
            RoleId = superAdminRole.Id
        }, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
