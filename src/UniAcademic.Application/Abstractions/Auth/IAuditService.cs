namespace UniAcademic.Application.Abstractions.Auth;

public interface IAuditService
{
    Task WriteAsync(
        string action,
        string entityType,
        string? entityId,
        object? detail = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}
