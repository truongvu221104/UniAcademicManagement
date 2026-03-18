namespace UniAcademic.Application.Abstractions.Common;

public interface IClientContextAccessor
{
    string? IpAddress { get; }

    string? UserAgent { get; }

    string ClientType { get; }
}
