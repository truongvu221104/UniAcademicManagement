namespace UniAcademic.Application.Abstractions.Auth;

public interface ICurrentStudentContext
{
    Task<Guid?> GetStudentProfileIdAsync(CancellationToken cancellationToken = default);

    Task<Guid> GetRequiredStudentProfileIdAsync(CancellationToken cancellationToken = default);
}
