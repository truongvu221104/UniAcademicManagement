using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.LecturerAssignments;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.LecturerAssignments;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Application.Features.LecturerAssignments;

public sealed class LecturerAssignmentService : ILecturerAssignmentService
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LecturerAssignmentService(
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

    public async Task<LecturerAssignmentModel> AssignAsync(AssignLecturerCommand command, CancellationToken cancellationToken = default)
    {
        var courseOffering = await RequireCourseOfferingAsync(command.CourseOfferingId, cancellationToken);
        var lecturerProfile = await RequireLecturerProfileAsync(command.LecturerProfileId, cancellationToken);

        var duplicateAssignment = await _dbContext.LecturerAssignments
            .AnyAsync(x => x.CourseOfferingId == courseOffering.Id && x.LecturerProfileId == lecturerProfile.Id, cancellationToken);
        if (duplicateAssignment)
        {
            throw new AuthException("Lecturer assignment already exists.");
        }

        if (command.IsPrimary)
        {
            var hasPrimary = await _dbContext.LecturerAssignments
                .AnyAsync(x => x.CourseOfferingId == courseOffering.Id && x.IsPrimary, cancellationToken);
            if (hasPrimary)
            {
                throw new AuthException("Course offering already has a primary lecturer.");
            }
        }

        var assignment = new LecturerAssignment
        {
            CourseOfferingId = courseOffering.Id,
            LecturerProfileId = lecturerProfile.Id,
            IsPrimary = command.IsPrimary,
            AssignedAtUtc = _dateTimeProvider.UtcNow,
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _dbContext.AddAsync(assignment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("lecturerassignment.assign", nameof(LecturerAssignment), assignment.Id.ToString(), new
        {
            assignment.CourseOfferingId,
            assignment.LecturerProfileId,
            assignment.IsPrimary
        }, _currentUser.UserId, cancellationToken);

        return Map(assignment, courseOffering, lecturerProfile);
    }

    public async Task UnassignAsync(UnassignLecturerCommand command, CancellationToken cancellationToken = default)
    {
        var assignment = await _dbContext.LecturerAssignments
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Lecturer assignment was not found.");

        _dbContext.Remove(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("lecturerassignment.unassign", nameof(LecturerAssignment), assignment.Id.ToString(), new
        {
            assignment.CourseOfferingId,
            assignment.LecturerProfileId,
            assignment.IsPrimary
        }, _currentUser.UserId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LecturerAssignmentModel>> GetListAsync(GetLecturerAssignmentsQuery query, CancellationToken cancellationToken = default)
    {
        var assignments = _dbContext.LecturerAssignments
            .AsNoTracking()
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.LecturerProfile)
                .ThenInclude(x => x!.Faculty)
            .AsQueryable();

        if (query.CourseOfferingId.HasValue)
        {
            assignments = assignments.Where(x => x.CourseOfferingId == query.CourseOfferingId.Value);
        }

        if (query.LecturerProfileId.HasValue)
        {
            assignments = assignments.Where(x => x.LecturerProfileId == query.LecturerProfileId.Value);
        }

        return await assignments
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.LecturerProfile != null ? x.LecturerProfile.Code : string.Empty)
            .Select(x => new LecturerAssignmentModel
            {
                Id = x.Id,
                CourseOfferingId = x.CourseOfferingId,
                CourseOfferingCode = x.CourseOffering != null ? x.CourseOffering.Code : string.Empty,
                CourseName = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Name : string.Empty,
                SemesterName = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.Name : string.Empty,
                LecturerProfileId = x.LecturerProfileId,
                LecturerCode = x.LecturerProfile != null ? x.LecturerProfile.Code : string.Empty,
                LecturerFullName = x.LecturerProfile != null ? x.LecturerProfile.FullName : string.Empty,
                FacultyName = x.LecturerProfile != null && x.LecturerProfile.Faculty != null ? x.LecturerProfile.Faculty.Name : string.Empty,
                IsPrimary = x.IsPrimary,
                AssignedAtUtc = x.AssignedAtUtc
            })
            .ToListAsync(cancellationToken);
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

    private async Task<LecturerProfile> RequireLecturerProfileAsync(Guid lecturerProfileId, CancellationToken cancellationToken)
    {
        if (lecturerProfileId == Guid.Empty)
        {
            throw new AuthException("Lecturer profile is required.");
        }

        var lecturerProfile = await _dbContext.LecturerProfiles
            .IgnoreQueryFilters()
            .Include(x => x.Faculty)
            .FirstOrDefaultAsync(x => x.Id == lecturerProfileId, cancellationToken);

        if (lecturerProfile is null || lecturerProfile.IsDeleted)
        {
            throw new AuthException("Lecturer profile was not found.");
        }

        if (!lecturerProfile.IsActive)
        {
            throw new AuthException("Lecturer profile is inactive.");
        }

        return lecturerProfile;
    }

    private static LecturerAssignmentModel Map(LecturerAssignment assignment, CourseOffering courseOffering, LecturerProfile lecturerProfile)
    {
        return new LecturerAssignmentModel
        {
            Id = assignment.Id,
            CourseOfferingId = assignment.CourseOfferingId,
            CourseOfferingCode = courseOffering.Code,
            CourseName = courseOffering.Course?.Name ?? string.Empty,
            SemesterName = courseOffering.Semester?.Name ?? string.Empty,
            LecturerProfileId = assignment.LecturerProfileId,
            LecturerCode = lecturerProfile.Code,
            LecturerFullName = lecturerProfile.FullName,
            FacultyName = lecturerProfile.Faculty?.Name ?? string.Empty,
            IsPrimary = assignment.IsPrimary,
            AssignedAtUtc = assignment.AssignedAtUtc
        };
    }
}
