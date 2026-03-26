using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Enrollments;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Enrollments;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Features.Enrollments;

public sealed class EnrollmentService : IEnrollmentService
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly int _maxCreditsPerSemester;

    public EnrollmentService(
        IAppDbContext dbContext,
        IAuditService auditService,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _maxCreditsPerSemester = int.TryParse(configuration["Enrollment:MaxCreditsPerSemester"], out var maxCreditsPerSemester)
            ? maxCreditsPerSemester
            : 24;
    }

    public async Task<EnrollmentModel> EnrollAsync(EnrollStudentCommand command, CancellationToken cancellationToken = default)
    {
        var studentProfile = await RequireStudentProfileAsync(command.StudentProfileId, cancellationToken);
        var courseOffering = await RequireCourseOfferingAsync(command.CourseOfferingId, cancellationToken);
        EnsureRosterIsOpen(courseOffering);
        var note = NormalizeNote(command.Note);
        var overrideReason = NormalizeOverrideReason(command.IsOverride, command.OverrideReason);

        var existingEnrollment = await _dbContext.Enrollments
            .Include(x => x.StudentProfile)
                .ThenInclude(x => x!.StudentClass)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .FirstOrDefaultAsync(
                x => x.StudentProfileId == studentProfile.Id && x.CourseOfferingId == courseOffering.Id,
                cancellationToken);

        if (existingEnrollment is not null && existingEnrollment.Status == EnrollmentStatus.Enrolled)
        {
            throw new AuthException("Enrollment already exists.");
        }

        if (!command.IsOverride)
        {
            await CheckPrerequisiteAsync(studentProfile, courseOffering, cancellationToken);
            await CheckTimeConflictAsync(studentProfile, courseOffering, cancellationToken);
            await CheckCreditLimitAsync(studentProfile, courseOffering, cancellationToken);
            await CheckRepeatRuleAsync(studentProfile, courseOffering, cancellationToken);
        }

        await EnsureCapacityAvailableAsync(courseOffering.Id, courseOffering.Capacity, cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var auditAction = command.IsOverride ? "enrollment.override" : "enrollment.enroll";

        if (existingEnrollment is not null)
        {
            existingEnrollment.Status = EnrollmentStatus.Enrolled;
            existingEnrollment.EnrolledAtUtc = now;
            existingEnrollment.DroppedAtUtc = null;
            existingEnrollment.Note = note;
            existingEnrollment.ModifiedBy = _currentUser.Username ?? "system";

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _auditService.WriteAsync(auditAction, nameof(Enrollment), existingEnrollment.Id.ToString(), new
            {
                existingEnrollment.StudentProfileId,
                existingEnrollment.CourseOfferingId,
                existingEnrollment.Status,
                IsOverride = command.IsOverride,
                OverrideReason = overrideReason
            }, _currentUser.UserId, cancellationToken);

            return Map(
                existingEnrollment,
                existingEnrollment.StudentProfile ?? studentProfile,
                existingEnrollment.CourseOffering ?? courseOffering);
        }

        var enrollment = new Enrollment
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = courseOffering.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = now,
            Note = note,
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _dbContext.AddAsync(enrollment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(auditAction, nameof(Enrollment), enrollment.Id.ToString(), new
        {
            enrollment.StudentProfileId,
            enrollment.CourseOfferingId,
            enrollment.Status,
            IsOverride = command.IsOverride,
            OverrideReason = overrideReason
        }, _currentUser.UserId, cancellationToken);

        return Map(enrollment, studentProfile, courseOffering);
    }

    public async Task DropAsync(DropEnrollmentCommand command, CancellationToken cancellationToken = default)
    {
        var enrollment = await _dbContext.Enrollments
            .Include(x => x.CourseOffering)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Enrollment was not found.");

        if (enrollment.Status == EnrollmentStatus.Dropped)
        {
            throw new AuthException("Enrollment was already dropped.");
        }

        EnsureRosterIsOpen(enrollment.CourseOffering);

        enrollment.Status = EnrollmentStatus.Dropped;
        enrollment.DroppedAtUtc = _dateTimeProvider.UtcNow;
        enrollment.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("enrollment.drop", nameof(Enrollment), enrollment.Id.ToString(), new
        {
            enrollment.StudentProfileId,
            enrollment.CourseOfferingId,
            enrollment.Status
        }, _currentUser.UserId, cancellationToken);
    }

    public async Task<EnrollmentModel> GetByIdAsync(GetEnrollmentByIdQuery query, CancellationToken cancellationToken = default)
    {
        var enrollment = await BuildEnrollmentQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw new AuthException("Enrollment was not found.");

        return Map(enrollment, enrollment.StudentProfile, enrollment.CourseOffering);
    }

    public async Task<IReadOnlyCollection<EnrollmentListItemModel>> GetListAsync(GetEnrollmentsQuery query, CancellationToken cancellationToken = default)
    {
        var enrollments = BuildEnrollmentQuery().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            enrollments = enrollments.Where(x =>
                (x.StudentProfile != null && (x.StudentProfile.StudentCode.Contains(keyword) || x.StudentProfile.FullName.Contains(keyword))) ||
                (x.CourseOffering != null && (x.CourseOffering.Code.Contains(keyword) ||
                    (x.CourseOffering.Course != null && x.CourseOffering.Course.Name.Contains(keyword)))));
        }

        if (query.StudentProfileId.HasValue)
        {
            enrollments = enrollments.Where(x => x.StudentProfileId == query.StudentProfileId.Value);
        }

        if (query.CourseOfferingId.HasValue)
        {
            enrollments = enrollments.Where(x => x.CourseOfferingId == query.CourseOfferingId.Value);
        }

        if (query.Status.HasValue)
        {
            enrollments = enrollments.Where(x => x.Status == query.Status.Value);
        }

        return await enrollments
            .OrderByDescending(x => x.EnrolledAtUtc)
            .Select(x => new EnrollmentListItemModel
            {
                Id = x.Id,
                StudentProfileId = x.StudentProfileId,
                StudentCode = x.StudentProfile != null ? x.StudentProfile.StudentCode : string.Empty,
                StudentFullName = x.StudentProfile != null ? x.StudentProfile.FullName : string.Empty,
                CourseOfferingId = x.CourseOfferingId,
                CourseOfferingCode = x.CourseOffering != null ? x.CourseOffering.Code : string.Empty,
                CourseName = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Name : string.Empty,
                SemesterName = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.Name : string.Empty,
                Status = x.Status,
                EnrolledAtUtc = x.EnrolledAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Enrollment> BuildEnrollmentQuery()
    {
        return _dbContext.Enrollments
            .Include(x => x.StudentProfile)
                .ThenInclude(x => x!.StudentClass)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester);
    }

    private async Task<StudentProfile> RequireStudentProfileAsync(Guid studentProfileId, CancellationToken cancellationToken)
    {
        if (studentProfileId == Guid.Empty)
        {
            throw new AuthException("Student profile is required.");
        }

        var studentProfile = await _dbContext.StudentProfiles
            .IgnoreQueryFilters()
            .Include(x => x.StudentClass)
            .FirstOrDefaultAsync(x => x.Id == studentProfileId, cancellationToken);

        if (studentProfile is null || studentProfile.IsDeleted)
        {
            throw new AuthException("Student profile was not found.");
        }

        return studentProfile;
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

    private async Task EnsureCapacityAvailableAsync(Guid courseOfferingId, int capacity, CancellationToken cancellationToken)
    {
        var enrolledCount = await _dbContext.Enrollments
            .CountAsync(x => x.CourseOfferingId == courseOfferingId && x.Status == EnrollmentStatus.Enrolled, cancellationToken);

        if (enrolledCount >= capacity)
        {
            throw new AuthException("Course offering capacity has been reached.");
        }
    }

    private async Task CheckPrerequisiteAsync(StudentProfile studentProfile, CourseOffering courseOffering, CancellationToken cancellationToken)
    {
        var prerequisiteCourseIds = await _dbContext.CoursePrerequisites
            .Where(x => x.CourseId == courseOffering.CourseId)
            .Select(x => x.PrerequisiteCourseId)
            .ToListAsync(cancellationToken);

        if (prerequisiteCourseIds.Count == 0)
        {
            return;
        }

        var passedCourseIds = await (
            from result in _dbContext.GradeResults
            join rosterItem in _dbContext.CourseOfferingRosterItems on result.RosterItemId equals rosterItem.Id
            join offering in _dbContext.CourseOfferings on result.CourseOfferingId equals offering.Id
            where rosterItem.StudentProfileId == studentProfile.Id && result.IsPassed
            select offering.CourseId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var hasMissingPrerequisite = prerequisiteCourseIds.Except(passedCourseIds).Any();
        if (hasMissingPrerequisite)
        {
            throw new AuthException("Student has not satisfied course prerequisites.");
        }
    }

    private async Task CheckTimeConflictAsync(StudentProfile studentProfile, CourseOffering courseOffering, CancellationToken cancellationToken)
    {
        if (!HasSchedule(courseOffering))
        {
            return;
        }

        var hasConflict = await (
            from enrollment in _dbContext.Enrollments
            join offering in _dbContext.CourseOfferings on enrollment.CourseOfferingId equals offering.Id
            where enrollment.StudentProfileId == studentProfile.Id
                && enrollment.Status == EnrollmentStatus.Enrolled
                && offering.SemesterId == courseOffering.SemesterId
                && offering.Id != courseOffering.Id
                && offering.DayOfWeek == courseOffering.DayOfWeek
                && offering.DayOfWeek >= 1
                && offering.DayOfWeek <= 7
                && offering.StartPeriod > 0
                && offering.EndPeriod >= offering.StartPeriod
                && courseOffering.StartPeriod <= offering.EndPeriod
                && offering.StartPeriod <= courseOffering.EndPeriod
            select offering.Id)
            .AnyAsync(cancellationToken);

        if (hasConflict)
        {
            throw new AuthException("Course offering schedule conflicts with another enrolled course offering.");
        }
    }

    private async Task CheckCreditLimitAsync(StudentProfile studentProfile, CourseOffering courseOffering, CancellationToken cancellationToken)
    {
        var currentCredits = await (
            from enrollment in _dbContext.Enrollments
            join offering in _dbContext.CourseOfferings on enrollment.CourseOfferingId equals offering.Id
            join course in _dbContext.Courses on offering.CourseId equals course.Id
            where enrollment.StudentProfileId == studentProfile.Id
                && enrollment.Status == EnrollmentStatus.Enrolled
                && offering.SemesterId == courseOffering.SemesterId
                && offering.Id != courseOffering.Id
            select course.Credits)
            .SumAsync(cancellationToken);

        var targetCredits = courseOffering.Course?.Credits ?? 0;
        if (currentCredits + targetCredits > _maxCreditsPerSemester)
        {
            throw new AuthException($"Enrollment exceeds the semester credit limit of {_maxCreditsPerSemester}.");
        }
    }

    private async Task CheckRepeatRuleAsync(StudentProfile studentProfile, CourseOffering courseOffering, CancellationToken cancellationToken)
    {
        var alreadyPassedCourse = await (
            from result in _dbContext.GradeResults
            join rosterItem in _dbContext.CourseOfferingRosterItems on result.RosterItemId equals rosterItem.Id
            join offering in _dbContext.CourseOfferings on result.CourseOfferingId equals offering.Id
            where rosterItem.StudentProfileId == studentProfile.Id
                && offering.CourseId == courseOffering.CourseId
                && result.IsPassed
            select result.Id)
            .AnyAsync(cancellationToken);

        if (alreadyPassedCourse)
        {
            throw new AuthException("Student has already passed this course.");
        }

        var alreadyEnrolledInSameCourse = await (
            from enrollment in _dbContext.Enrollments
            join offering in _dbContext.CourseOfferings on enrollment.CourseOfferingId equals offering.Id
            where enrollment.StudentProfileId == studentProfile.Id
                && enrollment.Status == EnrollmentStatus.Enrolled
                && offering.Id != courseOffering.Id
                && offering.CourseId == courseOffering.CourseId
            select enrollment.Id)
            .AnyAsync(cancellationToken);

        if (alreadyEnrolledInSameCourse)
        {
            throw new AuthException("Student is already enrolled in another offering of this course.");
        }
    }

    private static void EnsureRosterIsOpen(CourseOffering? courseOffering)
    {
        if (courseOffering?.IsRosterFinalized == true)
        {
            throw new AuthException("Course offering roster was already finalized.");
        }
    }

    private static EnrollmentModel Map(Enrollment enrollment, StudentProfile? studentProfile, CourseOffering? courseOffering)
    {
        return new EnrollmentModel
        {
            Id = enrollment.Id,
            StudentProfileId = enrollment.StudentProfileId,
            StudentCode = studentProfile?.StudentCode ?? string.Empty,
            StudentFullName = studentProfile?.FullName ?? string.Empty,
            StudentClassName = studentProfile?.StudentClass?.Name ?? string.Empty,
            CourseOfferingId = enrollment.CourseOfferingId,
            CourseOfferingCode = courseOffering?.Code ?? string.Empty,
            CourseCode = courseOffering?.Course?.Code ?? string.Empty,
            CourseName = courseOffering?.Course?.Name ?? string.Empty,
            SemesterName = courseOffering?.Semester?.Name ?? string.Empty,
            Status = enrollment.Status,
            EnrolledAtUtc = enrollment.EnrolledAtUtc,
            DroppedAtUtc = enrollment.DroppedAtUtc,
            Note = enrollment.Note
        };
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
            throw new AuthException("Enrollment note is invalid.");
        }

        return normalized;
    }

    private static string? NormalizeOverrideReason(bool isOverride, string? overrideReason)
    {
        if (!isOverride)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(overrideReason))
        {
            throw new AuthException("Override reason is required.");
        }

        var normalized = overrideReason.Trim();
        if (normalized.Length > 1000)
        {
            throw new AuthException("Override reason is invalid.");
        }

        return normalized;
    }

    private static bool HasSchedule(CourseOffering courseOffering)
        => courseOffering.DayOfWeek is >= 1 and <= 7
           && courseOffering.StartPeriod > 0
           && courseOffering.EndPeriod >= courseOffering.StartPeriod;
}
