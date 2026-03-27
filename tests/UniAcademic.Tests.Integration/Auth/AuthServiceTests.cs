using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniAcademic.Application.Models.Common;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Auth;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Identity;
using UniAcademic.Infrastructure.Options;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;

namespace UniAcademic.Tests.Integration.Auth;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_ForValidCredentials()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher();
        var user = await SeedUserAsync(dbContext, passwordHasher);
        var permission = new Permission
        {
            Code = PermissionConstants.Auth.ChangePassword,
            Module = "system.auth",
            Action = "change_password",
            Description = "Change password"
        };
        var role = new Role
        {
            Name = "SuperAdmin",
            NormalizedName = "SUPERADMIN",
            Description = "Role"
        };

        dbContext.Permissions.Add(permission);
        dbContext.Roles.Add(role);
        dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext, passwordHasher);

        var response = await service.LoginAsync(new AuthLoginRequest
        {
            UserNameOrEmail = "admin",
            Password = "Admin@123456",
            ClientType = "Api",
            IssueRefreshToken = true
        });

        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.NotEqual(Guid.Empty, response.SessionId);
        Assert.Equal("admin", response.User.Username);
        Assert.Contains(PermissionConstants.Auth.ChangePassword, response.User.Permissions);
    }

    [Fact]
    public async Task LoginAsync_ShouldNotIssueRefreshToken_ForWebClient()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher();
        await SeedUserAsync(dbContext, passwordHasher);

        var service = CreateAuthService(dbContext, passwordHasher);

        var response = await service.LoginAsync(new AuthLoginRequest
        {
            UserNameOrEmail = "admin",
            Password = "Admin@123456",
            ClientType = "Web",
            IssueRefreshToken = false
        });

        Assert.Equal(string.Empty, response.AccessToken);
        Assert.Equal(string.Empty, response.RefreshToken);
        Assert.NotEqual(Guid.Empty, response.SessionId);
    }

    [Fact]
    public async Task RefreshAsync_ShouldRotateRefreshToken_ForValidToken()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher();
        var user = await SeedUserAsync(dbContext, passwordHasher);
        var role = new Role
        {
            Name = "SuperAdmin",
            NormalizedName = "SUPERADMIN",
            Description = "Role"
        };
        dbContext.Roles.Add(role);
        dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext, passwordHasher);
        var loginResponse = await service.LoginAsync(new AuthLoginRequest
        {
            UserNameOrEmail = "admin",
            Password = "Admin@123456",
            ClientType = "Api",
            IssueRefreshToken = true
        });

        var refreshResponse = await service.RefreshAsync(new AuthRefreshTokenRequest
        {
            RefreshToken = loginResponse.RefreshToken
        });

        Assert.NotEqual(loginResponse.RefreshToken, refreshResponse.RefreshToken);
        Assert.False(string.IsNullOrWhiteSpace(refreshResponse.AccessToken));
    }

    [Fact]
    public async Task RefreshAsync_ShouldRevokeSession_WhenOldRefreshTokenIsReused()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher();
        var user = await SeedUserAsync(dbContext, passwordHasher);
        var role = new Role
        {
            Name = "SuperAdmin",
            NormalizedName = "SUPERADMIN",
            Description = "Role"
        };
        dbContext.Roles.Add(role);
        dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext, passwordHasher);
        var loginResponse = await service.LoginAsync(new AuthLoginRequest
        {
            UserNameOrEmail = "admin",
            Password = "Admin@123456",
            ClientType = "Api",
            IssueRefreshToken = true
        });

        var rotated = await service.RefreshAsync(new AuthRefreshTokenRequest
        {
            RefreshToken = loginResponse.RefreshToken
        });

        await Assert.ThrowsAsync<AuthException>(() => service.RefreshAsync(new AuthRefreshTokenRequest
        {
            RefreshToken = loginResponse.RefreshToken
        }));

        await Assert.ThrowsAsync<AuthException>(() => service.RefreshAsync(new AuthRefreshTokenRequest
        {
            RefreshToken = rotated.RefreshToken
        }));
    }

    [Fact]
    public async Task LogoutAsync_ShouldRejectRefreshToken_FromAnotherUser()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher();
        await SeedUserAsync(dbContext, passwordHasher);
        var otherUser = new User
        {
            Username = "other",
            NormalizedUsername = "OTHER",
            Email = "other@uniacademic.local",
            NormalizedEmail = "OTHER@UNIACADEMIC.LOCAL",
            DisplayName = "Other",
            PasswordHash = passwordHasher.HashPassword("Other@123456"),
            IsActive = true
        };

        dbContext.Users.Add(otherUser);
        await dbContext.SaveChangesAsync();

        var otherService = CreateAuthService(dbContext, passwordHasher);
        var otherLogin = await otherService.LoginAsync(new AuthLoginRequest
        {
            UserNameOrEmail = "other",
            Password = "Other@123456",
            ClientType = "Api",
            IssueRefreshToken = true
        });

        var actingService = CreateAuthService(
            dbContext,
            passwordHasher,
            new FakeCurrentUser
            {
                IsAuthenticatedValue = true,
                UserIdValue = Guid.NewGuid(),
                UsernameValue = "intruder"
            });

        await Assert.ThrowsAsync<AuthException>(() => actingService.LogoutAsync(new AuthLogoutRequest
        {
            RefreshToken = otherLogin.RefreshToken
        }));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<User> SeedUserAsync(AppDbContext dbContext, PasswordHasher passwordHasher)
    {
        var user = new User
        {
            Username = "admin",
            NormalizedUsername = "ADMIN",
            Email = "admin@uniacademic.local",
            NormalizedEmail = "ADMIN@UNIACADEMIC.LOCAL",
            DisplayName = "Admin",
            PasswordHash = passwordHasher.HashPassword("Admin@123456"),
            IsActive = true
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private static AuthService CreateAuthService(
        AppDbContext dbContext,
        PasswordHasher passwordHasher,
        FakeCurrentUser? currentUser = null)
    {
        return new AuthService(
            dbContext,
            passwordHasher,
            new JwtTokenService(Options.Create(new JwtOptions
            {
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                SigningKey = "12345678901234567890123456789012",
                AccessTokenMinutes = 30,
                RefreshTokenDays = 7
            })),
            new RefreshTokenService(),
            new PermissionService(dbContext),
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeEmailSender(),
            currentUser ?? new FakeCurrentUser(),
            new FakeDateTimeProvider(),
            new FakeClientContextAccessor(),
            Options.Create(new JwtOptions
            {
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                SigningKey = "12345678901234567890123456789012",
                AccessTokenMinutes = 30,
                RefreshTokenDays = 7
            }));
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserIdValue { get; init; }
        public string? UsernameValue { get; init; }
        public Guid? SessionIdValue { get; init; }
        public bool IsAuthenticatedValue { get; init; }
        public IReadOnlyCollection<string> PermissionsValue { get; init; } = [];

        public Guid? UserId => UserIdValue;
        public string? Username => UsernameValue;
        public Guid? SessionId => SessionIdValue;
        public bool IsAuthenticated => IsAuthenticatedValue;
        public IReadOnlyCollection<string> Permissions => PermissionsValue;
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 18, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "IntegrationTests";
        public string ClientType => "Tests";
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
