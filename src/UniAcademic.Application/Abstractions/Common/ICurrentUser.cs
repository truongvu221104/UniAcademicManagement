namespace UniAcademic.Application.Abstractions.Common;

public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Username { get; }

    Guid? SessionId { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Permissions { get; }
}
