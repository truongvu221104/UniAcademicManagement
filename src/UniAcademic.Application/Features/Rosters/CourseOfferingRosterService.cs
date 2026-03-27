using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.ExamHandoff;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Abstractions.Rosters;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Rosters;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Features.Rosters;

public sealed class CourseOfferingRosterService : ICourseOfferingRosterService
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IExamHandoffService _examHandoffService;

    public CourseOfferingRosterService(
        IAppDbContext dbContext,
        IAuditService auditService,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IExamHandoffService examHandoffService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _examHandoffService = examHandoffService;
    }

    public async Task<CourseOfferingRosterModel> GetByCourseOfferingIdAsync(GetCourseOfferingRosterQuery query, CancellationToken cancellationToken = default)
    {
        var courseOffering = await RequireCourseOfferingAsync(query.CourseOfferingId, cancellationToken);

        var snapshot = await _dbContext.CourseOfferingRosterSnapshots
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.CourseOfferingId == courseOffering.Id, cancellationToken);

        return Map(courseOffering, snapshot);
    }

    public async Task<CourseOfferingRosterModel> FinalizeAsync(FinalizeCourseOfferingRosterCommand command, CancellationToken cancellationToken = default)
    {
        var courseOffering = await RequireCourseOfferingAsync(command.CourseOfferingId, cancellationToken);
        if (courseOffering.IsRosterFinalized)
        {
            throw new AuthException("Course offering roster was already finalized.");
        }

        var note = NormalizeNote(command.Note);
        var now = _dateTimeProvider.UtcNow;
        var finalizedBy = _currentUser.Username ?? "system";

        var enrolledItems = await _dbContext.Enrollments
            .AsNoTracking()
            .Where(x => x.CourseOfferingId == courseOffering.Id && x.Status == EnrollmentStatus.Enrolled)
            .Include(x => x.StudentProfile)
                .ThenInclude(x => x!.StudentClass)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .OrderBy(x => x.StudentProfile!.StudentCode)
            .ToListAsync(cancellationToken);

        var snapshot = new CourseOfferingRosterSnapshot
        {
            CourseOfferingId = courseOffering.Id,
            FinalizedAtUtc = now,
            FinalizedBy = finalizedBy,
            ItemCount = enrolledItems.Count,
            Note = note,
            CreatedBy = finalizedBy
        };

        foreach (var enrollment in enrolledItems)
        {
            snapshot.Items.Add(new CourseOfferingRosterItem
            {
                EnrollmentId = enrollment.Id,
                StudentProfileId = enrollment.StudentProfileId,
                StudentCode = enrollment.StudentProfile?.StudentCode ?? string.Empty,
                StudentFullName = enrollment.StudentProfile?.FullName ?? string.Empty,
                StudentClassName = enrollment.StudentProfile?.StudentClass?.Name ?? string.Empty,
                CourseOfferingCode = enrollment.CourseOffering?.Code ?? string.Empty,
                CourseCode = enrollment.CourseOffering?.Course?.Code ?? string.Empty,
                CourseName = enrollment.CourseOffering?.Course?.Name ?? string.Empty,
                SemesterName = enrollment.CourseOffering?.Semester?.Name ?? string.Empty,
                CreatedBy = finalizedBy
            });
        }

        courseOffering.IsRosterFinalized = true;
        courseOffering.RosterFinalizedAtUtc = now;
        courseOffering.ModifiedBy = finalizedBy;

        await _dbContext.AddAsync(snapshot, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("courseofferingroster.finalize", nameof(CourseOfferingRosterSnapshot), snapshot.Id.ToString(), new
        {
            snapshot.CourseOfferingId,
            snapshot.ItemCount,
            snapshot.FinalizedAtUtc
        }, _currentUser.UserId, cancellationToken);

        try
        {
            await _examHandoffService.HandoffAsync(snapshot, cancellationToken);
        }
        catch
        {
            // Finalize stays committed even if UniTestSystem handoff fails unexpectedly.
        }

        return Map(courseOffering, snapshot);
    }

    public async Task<CourseOfferingRosterModel> ReopenAsync(ReopenCourseOfferingRosterCommand command, CancellationToken cancellationToken = default)
    {
        EnsureAdminCanReopen();

        var courseOffering = await RequireCourseOfferingAsync(command.CourseOfferingId, cancellationToken);
        if (!courseOffering.IsRosterFinalized)
        {
            throw new AuthException("Course offering roster has not been finalized.");
        }

        var snapshot = await _dbContext.CourseOfferingRosterSnapshots
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.CourseOfferingId == courseOffering.Id, cancellationToken);

        if (snapshot is null)
        {
            throw new AuthException("Course offering roster snapshot was not found.");
        }

        if (await _dbContext.AttendanceSessions.AnyAsync(x => x.CourseOfferingId == courseOffering.Id, cancellationToken))
        {
            throw new AuthException("Roster cannot be reopened because attendance sessions already exist.");
        }

        var hasGradeCategories = await _dbContext.GradeCategories
            .AnyAsync(x => x.CourseOfferingId == courseOffering.Id, cancellationToken);

        var hasGradeEntries = await _dbContext.GradeEntries
            .AnyAsync(x => x.GradeCategory!.CourseOfferingId == courseOffering.Id, cancellationToken);

        if (hasGradeCategories || hasGradeEntries)
        {
            throw new AuthException("Roster cannot be reopened because grades already exist.");
        }

        if (await _dbContext.GradeResults.AnyAsync(x => x.CourseOfferingId == courseOffering.Id, cancellationToken))
        {
            throw new AuthException("Roster cannot be reopened because grade results already exist.");
        }

        var handoffLogs = await _dbContext.ExamHandoffLogs
            .Where(x => x.CourseOfferingId == courseOffering.Id)
            .ToListAsync(cancellationToken);

        if (handoffLogs.Any(x => x.Status == ExamHandoffStatus.Success))
        {
            throw new AuthException("Roster cannot be reopened because exam handoff already succeeded.");
        }

        foreach (var handoffLog in handoffLogs)
        {
            _dbContext.Remove(handoffLog);
        }

        var reopenedBy = _currentUser.Username ?? "system";
        var reason = NormalizeNote(command.Reason);

        _dbContext.Remove(snapshot);
        courseOffering.IsRosterFinalized = false;
        courseOffering.RosterFinalizedAtUtc = null;
        courseOffering.ModifiedBy = reopenedBy;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("courseofferingroster.reopen", nameof(CourseOfferingRosterSnapshot), snapshot.Id.ToString(), new
        {
            snapshot.CourseOfferingId,
            snapshot.ItemCount,
            Reason = reason
        }, _currentUser.UserId, cancellationToken);

        return Map(courseOffering, null);
    }

    private async Task<CourseOffering> RequireCourseOfferingAsync(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        if (courseOfferingId == Guid.Empty)
        {
            throw new AuthException("Course offering is required.");
        }

        var courseOffering = await _dbContext.CourseOfferings
            .IgnoreQueryFilters()
            .Include(x => x.Course)
            .Include(x => x.Semester)
            .FirstOrDefaultAsync(x => x.Id == courseOfferingId, cancellationToken);

        if (courseOffering is null || courseOffering.IsDeleted)
        {
            throw new AuthException("Course offering was not found.");
        }

        return courseOffering;
    }

    private void EnsureAdminCanReopen()
    {
        if (!string.Equals(_currentUser.Username, "admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new AuthException("Only admin can reopen a finalized roster.");
        }
    }

    private static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var normalized = note.Trim();
        if (normalized.Length > 1000)
        {
            throw new AuthException("Roster note is invalid.");
        }

        return normalized;
    }

    private static CourseOfferingRosterModel Map(CourseOffering courseOffering, CourseOfferingRosterSnapshot? snapshot)
    {
        return new CourseOfferingRosterModel
        {
            CourseOfferingId = courseOffering.Id,
            CourseOfferingCode = courseOffering.Code,
            CourseName = courseOffering.Course?.Name ?? string.Empty,
            SemesterName = courseOffering.Semester?.Name ?? string.Empty,
            IsFinalized = courseOffering.IsRosterFinalized,
            FinalizedAtUtc = snapshot?.FinalizedAtUtc ?? courseOffering.RosterFinalizedAtUtc,
            FinalizedBy = snapshot?.FinalizedBy,
            ItemCount = snapshot?.ItemCount ?? 0,
            Note = snapshot?.Note,
            Items = snapshot?.Items
                .OrderBy(x => x.StudentCode)
                .Select(x => new CourseOfferingRosterItemModel
                {
                    EnrollmentId = x.EnrollmentId,
                    StudentProfileId = x.StudentProfileId,
                    StudentCode = x.StudentCode,
                    StudentFullName = x.StudentFullName,
                    StudentClassName = x.StudentClassName,
                    CourseOfferingCode = x.CourseOfferingCode,
                    CourseCode = x.CourseCode,
                    CourseName = x.CourseName,
                    SemesterName = x.SemesterName
                })
                .ToList() ?? []
        };
    }
}
