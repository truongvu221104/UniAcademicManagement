using System.Text.Json;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Domain.Entities.Identity;
using UniAcademic.Infrastructure.Persistence;

namespace UniAcademic.Infrastructure.Services.Auth;

public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _dbContext;
    private readonly IClientContextAccessor _clientContextAccessor;

    public AuditService(AppDbContext dbContext, IClientContextAccessor clientContextAccessor)
    {
        _dbContext = dbContext;
        _clientContextAccessor = clientContextAccessor;
    }

    public async Task WriteAsync(
        string action,
        string entityType,
        string? entityId,
        object? detail = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DetailJson = detail is null ? null : JsonSerializer.Serialize(detail),
            IpAddress = _clientContextAccessor.IpAddress,
            UserAgent = _clientContextAccessor.UserAgent,
            CreatedBy = userId?.ToString() ?? "anonymous"
        };

        await _dbContext.AuditLogs.AddAsync(log, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
