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
        var superAdminRole = await SeedRoleAsync(
            RoleConstants.SuperAdmin,
            "Bootstrap system administrator role",
            PermissionConstants.All,
            cancellationToken);

        await SeedRoleAsync(
            RoleConstants.Staff,
            "Academic operations role",
            BuildStaffPermissions(),
            cancellationToken);

        await SeedRoleAsync(
            RoleConstants.Student,
            "Student portal role",
            BuildStudentPermissions(),
            cancellationToken);

        await SeedRoleAsync(
            RoleConstants.Lecturer,
            "Lecturer portal role",
            BuildLecturerPermissions(),
            cancellationToken);

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

    private async Task<Role> SeedRoleAsync(
        string roleName,
        string description,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken)
    {
        var normalizedName = roleName.Trim().ToUpperInvariant();
        var existingRole = await _dbContext.Roles
            .Include(x => x.RolePermissions)
            .FirstOrDefaultAsync(x => x.NormalizedName == normalizedName, cancellationToken);

        if (existingRole is not null)
        {
            existingRole.Name = roleName;
            existingRole.Description = description;
            await EnsureRolePermissionsAsync(existingRole, permissions, cancellationToken);
            return existingRole;
        }

        var role = new Role
        {
            Name = roleName,
            NormalizedName = normalizedName,
            Description = description,
            CreatedBy = "seed"
        };

        await _dbContext.Roles.AddAsync(role, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await EnsureRolePermissionsAsync(role, permissions, cancellationToken);

        return role;
    }

    private async Task EnsureRolePermissionsAsync(Role role, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken)
    {
        var permissionsByCode = await _dbContext.Permissions
            .Where(x => permissionCodes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var missingPermissionCodes = permissionCodes
            .Where(x => !permissionsByCode.ContainsKey(x))
            .ToList();

        if (missingPermissionCodes.Count > 0)
        {
            throw new InvalidOperationException($"Permissions were not seeded for role '{role.Name}': {string.Join(", ", missingPermissionCodes)}");
        }

        var existingAssignments = await _dbContext.RolePermissions
            .Where(x => x.RoleId == role.Id)
            .Include(x => x.Permission)
            .ToListAsync(cancellationToken);

        var desiredPermissionIds = permissionsByCode.Values.Select(x => x.Id).ToHashSet();

        var missing = desiredPermissionIds
            .Except(existingAssignments.Select(x => x.PermissionId))
            .Select(permissionId => new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
            })
            .ToList();

        var extra = existingAssignments
            .Where(x => !desiredPermissionIds.Contains(x.PermissionId))
            .ToList();

        if (missing.Count == 0 && extra.Count == 0)
        {
            return;
        }

        if (extra.Count > 0)
        {
            _dbContext.RolePermissions.RemoveRange(extra);
        }

        await _dbContext.RolePermissions.AddRangeAsync(missing, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyCollection<string> BuildStaffPermissions()
    {
        return
        [
            PermissionConstants.Auth.Login,
            PermissionConstants.Auth.ManageSessions,
            PermissionConstants.Auth.ChangePassword,
            PermissionConstants.Faculties.View,
            PermissionConstants.StudentClasses.View,
            PermissionConstants.Courses.View,
            PermissionConstants.Semesters.View,
            PermissionConstants.CourseOfferings.View,
            PermissionConstants.CourseOfferings.Create,
            PermissionConstants.CourseOfferings.Edit,
            PermissionConstants.StudentProfiles.View,
            PermissionConstants.StudentProfiles.Create,
            PermissionConstants.StudentProfiles.Edit,
            PermissionConstants.Enrollments.View,
            PermissionConstants.Enrollments.Create,
            PermissionConstants.Enrollments.Delete,
            PermissionConstants.CourseOfferingRosters.View,
            PermissionConstants.CourseOfferingRosters.Finalize,
            PermissionConstants.CourseOfferingRosters.RetryHandoff,
            PermissionConstants.Attendance.View,
            PermissionConstants.Attendance.Create,
            PermissionConstants.Attendance.Edit,
            PermissionConstants.Grades.View,
            PermissionConstants.Grades.Create,
            PermissionConstants.Grades.Edit,
            PermissionConstants.CourseMaterials.View,
            PermissionConstants.CourseMaterials.Create,
            PermissionConstants.CourseMaterials.Edit,
            PermissionConstants.CourseMaterials.Download,
            PermissionConstants.GradeResults.View,
            PermissionConstants.GradeResults.Calculate,
            PermissionConstants.LecturerProfiles.View,
            PermissionConstants.LecturerProfiles.Create,
            PermissionConstants.LecturerProfiles.Edit,
            PermissionConstants.LecturerAssignments.View,
            PermissionConstants.LecturerAssignments.Assign,
            PermissionConstants.LecturerAssignments.Unassign,
            PermissionConstants.Transcripts.View
        ];
    }

    private static IReadOnlyCollection<string> BuildStudentPermissions()
    {
        return
        [
            PermissionConstants.Auth.Login,
            PermissionConstants.Auth.ManageSessions,
            PermissionConstants.Auth.ChangePassword,
            PermissionConstants.CourseOfferings.View,
            PermissionConstants.Enrollments.View,
            PermissionConstants.Enrollments.Create,
            PermissionConstants.Enrollments.Delete,
            PermissionConstants.Attendance.View,
            PermissionConstants.Grades.View,
            PermissionConstants.CourseMaterials.View,
            PermissionConstants.CourseMaterials.Download,
            PermissionConstants.GradeResults.View,
            PermissionConstants.Transcripts.View
        ];
    }

    private static IReadOnlyCollection<string> BuildLecturerPermissions()
    {
        return
        [
            PermissionConstants.Auth.Login,
            PermissionConstants.Auth.ManageSessions,
            PermissionConstants.Auth.ChangePassword,
            PermissionConstants.CourseOfferings.View,
            PermissionConstants.Attendance.View,
            PermissionConstants.Attendance.Create,
            PermissionConstants.Attendance.Edit,
            PermissionConstants.Grades.View,
            PermissionConstants.Grades.Create,
            PermissionConstants.Grades.Edit,
            PermissionConstants.CourseMaterials.View,
            PermissionConstants.CourseMaterials.Create,
            PermissionConstants.CourseMaterials.Edit,
            PermissionConstants.CourseMaterials.Download,
            PermissionConstants.GradeResults.View
        ];
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
