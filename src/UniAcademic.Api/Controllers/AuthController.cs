using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Auth;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.Auth;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(new AuthLoginRequest
            {
                UserNameOrEmail = request.UserNameOrEmail,
                Password = request.Password,
                RememberMe = request.RememberMe,
                ClientType = string.IsNullOrWhiteSpace(request.ClientType) ? "Api" : request.ClientType,
                DeviceName = request.DeviceName,
                IssueRefreshToken = true
            }, cancellationToken);

            return Ok(Map(response));
        }
        catch (AuthException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RefreshAsync(new AuthRefreshTokenRequest
            {
                RefreshToken = request.RefreshToken
            }, cancellationToken);
            return Ok(Map(response));
        }
        catch (AuthException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _authService.LogoutAsync(new AuthLogoutRequest
            {
                RefreshToken = request.RefreshToken
            }, cancellationToken);
            return NoContent();
        }
        catch (AuthException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        await _authService.LogoutAllAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var response = await _authService.GetCurrentUserAsync(cancellationToken);
        return Ok(Map(response));
    }

    [Authorize]
    [HttpGet("my-permissions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyPermissions(CancellationToken cancellationToken)
    {
        var response = await _authService.GetMyPermissionsAsync(cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = UniAcademic.Application.Security.PermissionConstants.PolicyPrefix + UniAcademic.Application.Security.PermissionConstants.Auth.ChangePassword)]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _authService.ChangePasswordAsync(new AuthChangePasswordRequest
            {
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword
            }, cancellationToken);
            return NoContent();
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static AuthResponse Map(AuthResult result)
    {
        return new AuthResponse
        {
            SessionId = result.SessionId,
            AccessToken = result.AccessToken,
            AccessTokenExpiresAtUtc = result.AccessTokenExpiresAtUtc,
            RefreshToken = result.RefreshToken,
            RefreshTokenExpiresAtUtc = result.RefreshTokenExpiresAtUtc,
            User = Map(result.User)
        };
    }

    private static CurrentUserResponse Map(CurrentUserModel user)
    {
        return new CurrentUserResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Roles = user.Roles,
            Permissions = user.Permissions
        };
    }
}
