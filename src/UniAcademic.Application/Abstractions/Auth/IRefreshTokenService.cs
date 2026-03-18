namespace UniAcademic.Application.Abstractions.Auth;

public interface IRefreshTokenService
{
    string GenerateToken();

    string HashToken(string token);
}
