using Microsoft.EntityFrameworkCore;
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

    public EnrollmentService(
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

    public async Task<EnrollmentModel> EnrollAsync(EnrollStudentCommand command, CancellationToken cancellationToken = default)
    {
        var studentProfile = await RequireStudentProfileAsync(command.StudentProfileId, cancellationToken);
        var courseOffering = await RequireCourseOfferingAsync(command.CourseOfferingId, cancellationToken);
        var note = NormalizeNote(command.Note);

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

        await EnsureCapacityAvailableAsync(courseOffering.Id, courseOffering.Capacity, cancellationToken);

        var now = _dateTimeProvider.UtcNow;

        if (existingEnrollment is not null)
        {
            existingEnrollment.Status = EnrollmentStatus.Enrolled;
            existingEnrollment.EnrolledAtUtc = now;
            existingEnrollment.DroppedAtUtc = null;
            existingEnrollment.Note = note;
            existingEnrollment.ModifiedBy = _currentUser.Username ?? "system";

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _auditService.WriteAsync("enrollment.enroll", nameof(Enrollment), existingEnrollment.Id.ToString(), new
            {
                existingEnrollment.StudentProfileId,
                existingEnrollment.CourseOfferingId,
                existingEnrollment.Status
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
        await _auditService.WriteAsync("enrollment.enroll", nameof(Enrollment), enrollment.Id.ToString(), new
        {
            enrollment.StudentProfileId,
            enrollment.CourseOfferingId,
            enrollment.Status
        }, _currentUser.UserId, cancellationToken);

        return Map(enrollment, studentProfile, courseOffering);
    }

    public async Task DropAsync(DropEnrollmentCommand command, CancellationToken cancellationToken = default)
    {
        var enrollment = await _dbContext.Enrollments.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Enrollment was not found.");

        if (enrollment.Status == EnrollmentStatus.Dropped)
        {
            throw new AuthException("Enrollment was already dropped.");
        }

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
}
