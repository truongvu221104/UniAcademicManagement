using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Auth;
using UniAcademic.Application.Security;
using UniAcademic.Web.Models.Account;

namespace UniAcademic.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var response = await _authService.LoginAsync(new AuthLoginRequest
            {
                UserNameOrEmail = model.UserNameOrEmail,
                Password = model.Password,
                RememberMe = model.RememberMe,
                ClientType = "Web",
                DeviceName = "Browser",
                IssueRefreshToken = false
            }, cancellationToken);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, response.User.UserId.ToString()),
                new(ClaimTypes.Name, response.User.Username),
                new(ClaimTypes.Email, response.User.Email),
                new(AppClaimTypes.DisplayName, response.User.DisplayName),
                new(AppClaimTypes.SessionId, response.SessionId.ToString())
            };

            claims.AddRange(response.User.Roles.Select(static role => new Claim(ClaimTypes.Role, role)));
            claims.AddRange(response.User.Permissions.Select(static permission => new Claim(AppClaimTypes.Permission, permission)));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
                });

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _authService.LogoutCurrentSessionAsync(cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
