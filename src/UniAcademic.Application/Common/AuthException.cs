namespace UniAcademic.Application.Common;

public sealed class AuthException : Exception
{
    public AuthException(string message)
        : base(message)
    {
    }
}
