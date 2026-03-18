using Xunit;
using UniAcademic.Infrastructure.Services.Auth;

namespace UniAcademic.Tests.Unit.Auth;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher = new();

    [Fact]
    public void HashPassword_ShouldVerify_WithOriginalPassword()
    {
        var password = "Admin@123456";
        var hash = _passwordHasher.HashPassword(password);

        var result = _passwordHasher.Verify(password, hash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WithWrongPassword()
    {
        var hash = _passwordHasher.HashPassword("Admin@123456");

        var result = _passwordHasher.Verify("WrongPassword", hash);

        Assert.False(result);
    }
}
