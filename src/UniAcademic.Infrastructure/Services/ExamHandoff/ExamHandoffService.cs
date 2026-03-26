using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.ExamHandoff;
using UniAcademic.Application.Models.ExamHandoff;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;

namespace UniAcademic.Infrastructure.Services.ExamHandoff;

public sealed class ExamHandoffService : IExamHandoffService
{
    private const string ClientName = "UniTestSystem";

    private readonly AppDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;

    public ExamHandoffService(
        AppDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IAuditService auditService,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    public async Task HandoffAsync(CourseOfferingRosterSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        try
        {
            var rosterSnapshot = await LoadSnapshotAsync(snapshot.Id, cancellationToken);
            if (rosterSnapshot is null)
            {
                await _auditService.WriteAsync(
                    "courseofferingroster.handoff",
                    nameof(CourseOfferingRosterSnapshot),
                    snapshot.Id.ToString(),
                    new
                    {
                        snapshot.CourseOfferingId,
                        SnapshotId = snapshot.Id,
                        Status = ExamHandoffStatus.Failed.ToString(),
                        Message = "Roster snapshot was not found."
                    },
                    _currentUser.UserId,
                    cancellationToken);
                return;
            }

            await SendAsync(rosterSnapshot, "courseofferingroster.handoff", cancellationToken);
        }
        catch (Exception ex)
        {
            await TryWriteFallbackAuditAsync("courseofferingroster.handoff", snapshot.Id.ToString(), snapshot.CourseOfferingId, ex, cancellationToken);
        }
    }

    public async Task RetryHandoffAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await _dbContext.CourseOfferingRosterSnapshotsSet
                .AsNoTracking()
                .Include(x => x.CourseOffering)
                    .ThenInclude(x => x!.Course)
                .Include(x => x.CourseOffering)
                    .ThenInclude(x => x!.Semester)
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.CourseOfferingId == courseOfferingId, cancellationToken);

            if (snapshot is null)
            {
                await _auditService.WriteAsync(
                    "courseofferingroster.handoff.retry",
                    nameof(CourseOfferingRosterSnapshot),
                    courseOfferingId.ToString(),
                    new
                    {
                        CourseOfferingId = courseOfferingId,
                        Status = ExamHandoffStatus.Failed.ToString(),
                        Message = "Roster snapshot was not found."
                    },
                    _currentUser.UserId,
                    cancellationToken);
                return;
            }

            await SendAsync(snapshot, "courseofferingroster.handoff.retry", cancellationToken);
        }
        catch (Exception ex)
        {
            await TryWriteFallbackAuditAsync("courseofferingroster.handoff.retry", courseOfferingId.ToString(), courseOfferingId, ex, cancellationToken);
        }
    }

    public async Task<ExamHandoffLogModel?> GetLatestStatusAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
    {
        var log = await _dbContext.ExamHandoffLogsSet
            .AsNoTracking()
            .Where(x => x.CourseOfferingId == courseOfferingId)
            .OrderByDescending(x => x.SentAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return log is null ? null : Map(log);
    }

    private async Task SendAsync(CourseOfferingRosterSnapshot snapshot, string auditAction, CancellationToken cancellationToken)
    {
        var attemptedBy = _currentUser.Username ?? snapshot.FinalizedBy ?? "system";
        var log = new ExamHandoffLog
        {
            CourseOfferingId = snapshot.CourseOfferingId,
            RosterSnapshotId = snapshot.Id,
            Status = ExamHandoffStatus.Pending,
            SentAtUtc = DateTime.UtcNow,
            CreatedBy = attemptedBy
        };

        await _dbContext.ExamHandoffLogsSet.AddAsync(log, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var client = _httpClientFactory.CreateClient(ClientName);
            var response = await client.PostAsJsonAsync("/api/exam-rosters", BuildPayload(snapshot), cancellationToken);

            log.ResponseCode = (int)response.StatusCode;
            log.Status = response.IsSuccessStatusCode ? ExamHandoffStatus.Success : ExamHandoffStatus.Failed;
            log.ErrorMessage = response.IsSuccessStatusCode ? null : await ReadErrorAsync(response, cancellationToken);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            log.Status = ExamHandoffStatus.Failed;
            log.ErrorMessage = $"UniTestSystem handoff timed out: {ex.Message}";
        }
        catch (Exception ex)
        {
            log.Status = ExamHandoffStatus.Failed;
            log.ErrorMessage = Truncate(ex.Message, 2000);
        }

        log.ModifiedBy = attemptedBy;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            auditAction,
            nameof(ExamHandoffLog),
            log.Id.ToString(),
            new
            {
                log.CourseOfferingId,
                log.RosterSnapshotId,
                Status = log.Status.ToString(),
                log.SentAtUtc,
                log.ResponseCode,
                log.ErrorMessage
            },
            _currentUser.UserId,
            cancellationToken);
    }

    private async Task<CourseOfferingRosterSnapshot?> LoadSnapshotAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        return await _dbContext.CourseOfferingRosterSnapshotsSet
            .AsNoTracking()
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == snapshotId, cancellationToken);
    }

    private static object BuildPayload(CourseOfferingRosterSnapshot snapshot)
    {
        return new
        {
            SnapshotId = snapshot.Id,
            snapshot.CourseOfferingId,
            snapshot.FinalizedAtUtc,
            snapshot.FinalizedBy,
            CourseOfferingCode = snapshot.CourseOffering?.Code ?? string.Empty,
            CourseCode = snapshot.CourseOffering?.Course?.Code ?? string.Empty,
            CourseName = snapshot.CourseOffering?.Course?.Name ?? string.Empty,
            SemesterCode = snapshot.CourseOffering?.Semester?.Code ?? string.Empty,
            SemesterName = snapshot.CourseOffering?.Semester?.Name ?? string.Empty,
            StudentCount = snapshot.ItemCount,
            Students = snapshot.Items
                .OrderBy(x => x.StudentCode)
                .Select(x => new
                {
                    x.StudentProfileId,
                    x.StudentCode,
                    x.StudentFullName,
                    x.StudentClassName
                })
                .ToList()
        };
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return $"UniTestSystem returned HTTP {(int)response.StatusCode}.";
        }

        return Truncate(body, 2000);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static ExamHandoffLogModel Map(ExamHandoffLog entity)
    {
        return new ExamHandoffLogModel
        {
            Id = entity.Id,
            CourseOfferingId = entity.CourseOfferingId,
            RosterSnapshotId = entity.RosterSnapshotId,
            Status = entity.Status.ToString(),
            SentAtUtc = entity.SentAtUtc,
            ResponseCode = entity.ResponseCode,
            ErrorMessage = entity.ErrorMessage
        };
    }

    private async Task TryWriteFallbackAuditAsync(
        string action,
        string entityId,
        Guid courseOfferingId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            await _auditService.WriteAsync(
                action,
                nameof(CourseOfferingRosterSnapshot),
                entityId,
                new
                {
                    CourseOfferingId = courseOfferingId,
                    Status = ExamHandoffStatus.Failed.ToString(),
                    Message = Truncate(exception.Message, 2000)
                },
                _currentUser.UserId,
                cancellationToken);
        }
        catch
        {
            // Swallow by design: exam handoff must never break roster finalization or retry callers.
        }
    }
}
