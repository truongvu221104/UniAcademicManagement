namespace UniAcademic.Application.Abstractions.Auth;

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool Verify(string password, string passwordHash);
}
