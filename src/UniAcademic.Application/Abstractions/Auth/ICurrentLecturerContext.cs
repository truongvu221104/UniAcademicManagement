namespace UniAcademic.Application.Abstractions.Auth;

public interface ICurrentLecturerContext
{
    Task<Guid?> GetLecturerProfileIdAsync(CancellationToken cancellationToken = default);

    Task<Guid> GetRequiredLecturerProfileIdAsync(CancellationToken cancellationToken = default);
}
