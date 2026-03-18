using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Auth;
using UniAcademic.Domain.Entities.Identity;
using UniAcademic.Infrastructure.Options;
using UniAcademic.Infrastructure.Persistence;

namespace UniAcademic.Infrastructure.Services.Auth;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IClientContextAccessor _clientContextAccessor;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IPermissionService permissionService,
        IAuditService auditService,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IClientContextAccessor clientContextAccessor,
        IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _permissionService = permissionService;
        _auditService = auditService;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _clientContextAccessor = clientContextAccessor;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResult> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedInput = request.UserNameOrEmail.Trim().ToUpperInvariant();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                x => x.NormalizedUsername == normalizedInput || x.NormalizedEmail == normalizedInput,
                cancellationToken);

        if (user is null || !user.IsActive || user.IsLocked || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            if (user is not null)
            {
                user.FailedLoginCount++;
                user.ModifiedBy = "system";
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await _auditService.WriteAsync(
                "auth.login.failed",
                nameof(User),
                user?.Id.ToString(),
                new { request.UserNameOrEmail },
                user?.Id,
                cancellationToken);

            throw new AuthException("Invalid username/email or password.");
        }

        user.FailedLoginCount = 0;
        user.LastLoginAtUtc = _dateTimeProvider.UtcNow;
        user.ModifiedBy = user.Username;

        var session = new UserSession
        {
            UserId = user.Id,
            ClientType = string.IsNullOrWhiteSpace(request.ClientType) ? _clientContextAccessor.ClientType : request.ClientType,
            DeviceName = request.DeviceName,
            IpAddress = _clientContextAccessor.IpAddress,
            UserAgent = _clientContextAccessor.UserAgent,
            StartedAtUtc = _dateTimeProvider.UtcNow,
            LastSeenAtUtc = _dateTimeProvider.UtcNow,
            CreatedBy = user.Username
        };

        await _dbContext.UserSessions.AddAsync(session, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await CreateAuthResponseAsync(user, session, request.IssueRefreshToken, cancellationToken);

        await _auditService.WriteAsync(
            "auth.login.succeeded",
            nameof(User),
            user.Id.ToString(),
            new { session.Id, session.ClientType },
            user.Id,
            cancellationToken);

        return response;
    }

    public async Task<AuthResult> RefreshAsync(AuthRefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenService.HashToken(request.RefreshToken);
        var refreshToken = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .Include(x => x.UserSession)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            throw new AuthException("Refresh token is invalid or expired.");
        }

        if (refreshToken.RevokedAtUtc.HasValue)
        {
            if (!string.IsNullOrWhiteSpace(refreshToken.ReplacedByTokenHash))
            {
                await RevokeSessionAsync(refreshToken.UserSessionId, refreshToken.UserId, "auth.refresh.replay_detected", cancellationToken);
            }

            throw new AuthException("Refresh token is invalid or expired.");
        }

        if (refreshToken.ExpiresAtUtc <= _dateTimeProvider.UtcNow || refreshToken.UserSession.IsRevoked)
        {
            throw new AuthException("Refresh token is invalid or expired.");
        }

        var user = refreshToken.User;
        refreshToken.RevokedAtUtc = _dateTimeProvider.UtcNow;
        refreshToken.RevokedByIp = _clientContextAccessor.IpAddress;
        refreshToken.ModifiedBy = user.Username;
        refreshToken.UserSession.LastSeenAtUtc = _dateTimeProvider.UtcNow;

        var response = await CreateAuthResponseAsync(user, refreshToken.UserSession, true, cancellationToken);
        refreshToken.ReplacedByTokenHash = _refreshTokenService.HashToken(response.RefreshToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            "auth.refresh.succeeded",
            nameof(User),
            user.Id.ToString(),
            new { refreshToken.UserSessionId },
            user.Id,
            cancellationToken);

        return response;
    }

    public async Task LogoutAsync(AuthLogoutRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var tokenHash = _refreshTokenService.HashToken(request.RefreshToken);
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            throw new AuthException("Refresh token is invalid.");
        }

        if (refreshToken.UserId != _currentUser.UserId)
        {
            await _auditService.WriteAsync(
                "auth.logout.denied",
                nameof(RefreshToken),
                refreshToken.Id.ToString(),
                new { refreshToken.UserId, CurrentUserId = _currentUser.UserId },
                _currentUser.UserId,
                cancellationToken);

            throw new AuthException("Refresh token is invalid.");
        }

        await RevokeSessionAsync(refreshToken.UserSessionId, _currentUser.UserId, "auth.logout", cancellationToken);
    }

    public async Task LogoutCurrentSessionAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        if (!_currentUser.SessionId.HasValue)
        {
            return;
        }

        await RevokeSessionAsync(_currentUser.SessionId.Value, _currentUser.UserId, "auth.logout", cancellationToken);
    }

    public async Task LogoutAllAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var now = _dateTimeProvider.UtcNow;
        var refreshTokens = await _dbContext.RefreshTokens
            .Where(x => x.UserId == _currentUser.UserId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in refreshTokens)
        {
            refreshToken.RevokedAtUtc = now;
            refreshToken.RevokedByIp = _clientContextAccessor.IpAddress;
            refreshToken.ModifiedBy = _currentUser.Username;
        }

        var sessions = await _dbContext.UserSessions
            .Where(x => x.UserId == _currentUser.UserId && !x.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.EndedAtUtc = now;
            session.ModifiedBy = _currentUser.Username;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("auth.logout_all", nameof(User), _currentUser.UserId!.Value.ToString(), null, _currentUser.UserId, cancellationToken);
    }

    public async Task<CurrentUserModel> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == _currentUser.UserId, cancellationToken)
            ?? throw new AuthException("Current user was not found.");

        return await BuildCurrentUserAsync(user, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetMyPermissionsAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return await _permissionService.GetPermissionsAsync(_currentUser.UserId!.Value, cancellationToken);
    }

    public async Task ChangePasswordAsync(AuthChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == _currentUser.UserId, cancellationToken)
            ?? throw new AuthException("Current user was not found.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new AuthException("Current password is invalid.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.ModifiedBy = user.Username;

        var activeRefreshTokens = await _dbContext.RefreshTokens
            .Where(x => x.UserId == user.Id && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.RevokedAtUtc = _dateTimeProvider.UtcNow;
            refreshToken.RevokedByIp = _clientContextAccessor.IpAddress;
            refreshToken.ModifiedBy = user.Username;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("auth.change_password", nameof(User), user.Id.ToString(), null, user.Id, cancellationToken);
    }

    private async Task<AuthResult> CreateAuthResponseAsync(
        User user,
        UserSession session,
        bool issueRefreshToken,
        CancellationToken cancellationToken)
    {
        var roles = await _permissionService.GetRolesAsync(user.Id, cancellationToken);
        var permissions = await _permissionService.GetPermissionsAsync(user.Id, cancellationToken);
        string accessToken = string.Empty;
        DateTime? accessTokenExpiresAtUtc = null;
        string refreshTokenValue = string.Empty;
        DateTime? refreshTokenExpiresAtUtc = null;

        if (issueRefreshToken)
        {
            var jwtResult = _jwtTokenService.CreateAccessToken(user, roles, permissions, session.Id);
            accessToken = jwtResult.AccessToken;
            accessTokenExpiresAtUtc = jwtResult.ExpiresAtUtc;

            refreshTokenValue = _refreshTokenService.GenerateToken();
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                UserSessionId = session.Id,
                TokenHash = _refreshTokenService.HashToken(refreshTokenValue),
                ExpiresAtUtc = _dateTimeProvider.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
                CreatedByIp = _clientContextAccessor.IpAddress,
                UserAgent = _clientContextAccessor.UserAgent,
                CreatedBy = user.Username
            };

            await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            refreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc;
        }

        return new AuthResult
        {
            SessionId = session.Id,
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc,
            RefreshToken = refreshTokenValue,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc,
            User = new CurrentUserModel
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Roles = roles,
                Permissions = permissions
            }
        };
    }

    private async Task<CurrentUserModel> BuildCurrentUserAsync(User user, CancellationToken cancellationToken)
    {
        var roles = await _permissionService.GetRolesAsync(user.Id, cancellationToken);
        var permissions = await _permissionService.GetPermissionsAsync(user.Id, cancellationToken);

        return new CurrentUserModel
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Roles = roles,
            Permissions = permissions
        };
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new AuthException("Authentication is required.");
        }
    }

    private async Task RevokeSessionAsync(
        Guid sessionId,
        Guid? actorUserId,
        string auditAction,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.UserSessions.FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return;
        }

        var now = _dateTimeProvider.UtcNow;
        session.IsRevoked = true;
        session.EndedAtUtc = session.EndedAtUtc ?? now;
        session.LastSeenAtUtc = now;
        session.ModifiedBy = _currentUser.Username ?? actorUserId?.ToString() ?? "system";

        var activeRefreshTokens = await _dbContext.RefreshTokens
            .Where(x => x.UserSessionId == sessionId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.RevokedAtUtc = now;
            refreshToken.RevokedByIp = _clientContextAccessor.IpAddress;
            refreshToken.ModifiedBy = _currentUser.Username ?? actorUserId?.ToString() ?? "system";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(auditAction, nameof(UserSession), session.Id.ToString(), null, actorUserId, cancellationToken);
    }
}
