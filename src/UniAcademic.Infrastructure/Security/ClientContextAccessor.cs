using Microsoft.AspNetCore.Http;
using UniAcademic.Application.Abstractions.Common;

namespace UniAcademic.Infrastructure.Security;

public sealed class ClientContextAccessor : IClientContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public string ClientType => _httpContextAccessor.HttpContext?.Request.Headers["X-Client-Type"].ToString() switch
    {
        { Length: > 0 } headerValue => headerValue,
        _ => "Http"
    };
}
