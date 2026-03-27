using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Attendance;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Attendance;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Features.Attendance;

public sealed class AttendanceService : IAttendanceService
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AttendanceService(
        IAppDbContext dbContext,
        IAuditService auditService,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AttendanceSessionModel> CreateSessionAsync(CreateAttendanceSessionCommand command, CancellationToken cancellationToken = default)
    {
        var courseOffering = await RequireCourseOfferingAsync(command.CourseOfferingId, cancellationToken);
        EnsureRosterFinalized(courseOffering);
        var rosterSnapshot = await RequireRosterSnapshotAsync(courseOffering.Id, cancellationToken);
        var sessionDate = NormalizeSessionDate(command.SessionDate);
        var sessionNo = NormalizeSessionNo(command.SessionNo);
        var title = NormalizeTitle(command.Title);
        var note = NormalizeNote(command.Note);

        var duplicateExists = await _dbContext.AttendanceSessions
            .AnyAsync(
                x => x.CourseOfferingId == courseOffering.Id
                    && x.SessionNo == sessionNo,
                cancellationToken);

        if (duplicateExists)
        {
            throw new AuthException("Attendance session number already exists for this class.");
        }

        var createdBy = _currentUser.Username ?? "system";
        var session = new AttendanceSession
        {
            CourseOfferingId = courseOffering.Id,
            CourseOfferingRosterSnapshotId = rosterSnapshot.Id,
            SessionDate = sessionDate,
            SessionNo = sessionNo,
            Title = title,
            Note = note,
            CreatedBy = createdBy
        };

        foreach (var rosterItem in rosterSnapshot.Items.OrderBy(x => x.StudentCode))
        {
            session.Records.Add(new AttendanceRecord
            {
                RosterItemId = rosterItem.Id,
                Status = AttendanceStatus.Unmarked,
                CreatedBy = createdBy
            });
        }

        await _dbContext.AddAsync(session, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("attendance.session.create", nameof(AttendanceSession), session.Id.ToString(), new
        {
            session.CourseOfferingId,
            session.SessionDate,
            session.SessionNo,
            RecordCount = session.Records.Count
        }, _currentUser.UserId, cancellationToken);

        return await GetByIdAsync(new GetAttendanceSessionByIdQuery { Id = session.Id }, cancellationToken);
    }

    public async Task<AttendanceSessionModel> UpdateSessionAsync(UpdateAttendanceSessionCommand command, CancellationToken cancellationToken = default)
    {
        var session = await BuildSessionQuery()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Attendance session was not found.");

        var sessionNo = NormalizeSessionNo(command.SessionNo);
        var title = NormalizeTitle(command.Title);
        var note = NormalizeNote(command.Note);

        var duplicateExists = await _dbContext.AttendanceSessions
            .AnyAsync(
                x => x.CourseOfferingId == session.CourseOfferingId
                    && x.Id != session.Id
                    && x.SessionNo == sessionNo,
                cancellationToken);

        if (duplicateExists)
        {
            throw new AuthException("Attendance session number already exists for this class.");
        }

        session.SessionNo = sessionNo;
        session.Title = title;
        session.Note = note;
        session.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("attendance.session.update", nameof(AttendanceSession), session.Id.ToString(), new
        {
            session.CourseOfferingId,
            session.SessionDate,
            session.SessionNo,
            session.Title
        }, _currentUser.UserId, cancellationToken);

        return Map(session);
    }

    public async Task<AttendanceSessionModel> UpdateRecordsAsync(UpdateAttendanceRecordsCommand command, CancellationToken cancellationToken = default)
    {
        var session = await BuildSessionQuery()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Attendance session was not found.");

        var sessionNo = NormalizeSessionNo(command.SessionNo);
        var title = NormalizeTitle(command.Title);
        var note = NormalizeNote(command.Note);

        var duplicateExists = await _dbContext.AttendanceSessions
            .AnyAsync(
                x => x.CourseOfferingId == session.CourseOfferingId
                    && x.Id != session.Id
                    && x.SessionNo == sessionNo,
                cancellationToken);

        if (duplicateExists)
        {
            throw new AuthException("Attendance session number already exists for this class.");
        }

        if (command.Records.Count == 0)
        {
            throw new AuthException("Attendance records are required.");
        }

        var duplicatedRosterItemId = command.Records
            .GroupBy(x => x.RosterItemId)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicatedRosterItemId is not null)
        {
            throw new AuthException("Attendance records contain duplicates.");
        }

        var recordsByRosterItemId = session.Records.ToDictionary(x => x.RosterItemId);
        var modifiedBy = _currentUser.Username ?? "system";

        foreach (var item in command.Records)
        {
            if (item.RosterItemId == Guid.Empty)
            {
                throw new AuthException("Roster item is required.");
            }

            if (!recordsByRosterItemId.TryGetValue(item.RosterItemId, out var record))
            {
                throw new AuthException("Attendance record does not belong to the session.");
            }

            record.Status = NormalizeStatus(item.Status);
            record.Note = NormalizeNote(item.Note);
            record.ModifiedBy = modifiedBy;
        }

        session.SessionNo = sessionNo;
        session.Title = title;
        session.Note = note;
        session.ModifiedBy = modifiedBy;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("attendance.records.update", nameof(AttendanceSession), session.Id.ToString(), new
        {
            session.CourseOfferingId,
            session.SessionDate,
            session.SessionNo,
            UpdatedCount = command.Records.Count
        }, _currentUser.UserId, cancellationToken);

        return Map(session);
    }

    public async Task<AttendanceSessionModel> GetByIdAsync(GetAttendanceSessionByIdQuery query, CancellationToken cancellationToken = default)
    {
        var session = await BuildSessionQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw new AuthException("Attendance session was not found.");

        return Map(session);
    }

    public async Task<IReadOnlyCollection<AttendanceSessionListItemModel>> GetListAsync(GetAttendanceSessionsQuery query, CancellationToken cancellationToken = default)
    {
        var sessions = _dbContext.AttendanceSessions
            .AsNoTracking()
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.Records)
            .AsQueryable();

        if (query.CourseOfferingId.HasValue)
        {
            sessions = sessions.Where(x => x.CourseOfferingId == query.CourseOfferingId.Value);
        }

        return await sessions
            .OrderByDescending(x => x.SessionDate)
            .ThenByDescending(x => x.SessionNo)
            .Select(x => new AttendanceSessionListItemModel
            {
                Id = x.Id,
                CourseOfferingId = x.CourseOfferingId,
                CourseOfferingCode = x.CourseOffering != null ? x.CourseOffering.Code : string.Empty,
                CourseName = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Name : string.Empty,
                SemesterName = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.Name : string.Empty,
                SessionDate = x.SessionDate,
                SessionNo = x.SessionNo,
                Title = x.Title,
                RecordCount = x.Records.Count
            })
            .ToListAsync(cancellationToken);
    }

    private IQueryable<AttendanceSession> BuildSessionQuery()
    {
        return _dbContext.AttendanceSessions
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.Records)
                .ThenInclude(x => x.RosterItem);
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

    private async Task<CourseOfferingRosterSnapshot> RequireRosterSnapshotAsync(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        var snapshot = await _dbContext.CourseOfferingRosterSnapshots
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.CourseOfferingId == courseOfferingId, cancellationToken);

        if (snapshot is null)
        {
            throw new AuthException("Course offering roster snapshot was not found.");
        }

        return snapshot;
    }

    private static void EnsureRosterFinalized(CourseOffering courseOffering)
    {
        if (!courseOffering.IsRosterFinalized)
        {
            throw new AuthException("Course offering roster was not finalized.");
        }
    }

    private static DateTime NormalizeSessionDate(DateTime sessionDate)
    {
        if (sessionDate == default)
        {
            throw new AuthException("Attendance session date is required.");
        }

        return sessionDate.Date;
    }

    private static int NormalizeSessionNo(int sessionNo)
    {
        if (sessionNo <= 0)
        {
            throw new AuthException("Attendance session number is invalid.");
        }

        return sessionNo;
    }

    private static AttendanceStatus NormalizeStatus(AttendanceStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new AuthException("Attendance status is invalid.");
        }

        return status;
    }

    private static string? NormalizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var normalized = title.Trim();
        if (normalized.Length > 200)
        {
            throw new AuthException("Attendance session title is invalid.");
        }

        return normalized;
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
            throw new AuthException("Attendance note is invalid.");
        }

        return normalized;
    }

    private static AttendanceSessionModel Map(AttendanceSession session)
    {
        return new AttendanceSessionModel
        {
            Id = session.Id,
            CourseOfferingId = session.CourseOfferingId,
            CourseOfferingCode = session.CourseOffering?.Code ?? string.Empty,
            CourseName = session.CourseOffering?.Course?.Name ?? string.Empty,
            SemesterName = session.CourseOffering?.Semester?.Name ?? string.Empty,
            CourseOfferingRosterSnapshotId = session.CourseOfferingRosterSnapshotId,
            SessionDate = session.SessionDate,
            SessionNo = session.SessionNo,
            Title = session.Title,
            Note = session.Note,
            RecordCount = session.Records.Count,
            Records = session.Records
                .OrderBy(x => x.RosterItem != null ? x.RosterItem.StudentCode : string.Empty)
                .Select(x => new AttendanceRecordModel
                {
                    Id = x.Id,
                    RosterItemId = x.RosterItemId,
                    StudentProfileId = x.RosterItem?.StudentProfileId ?? Guid.Empty,
                    StudentCode = x.RosterItem?.StudentCode ?? string.Empty,
                    StudentFullName = x.RosterItem?.StudentFullName ?? string.Empty,
                    StudentClassName = x.RosterItem?.StudentClassName ?? string.Empty,
                    Status = x.Status,
                    Note = x.Note
                })
                .ToList()
        };
    }
}
