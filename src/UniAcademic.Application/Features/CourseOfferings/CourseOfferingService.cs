using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.CourseOfferings;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.CourseOfferings;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Features.CourseOfferings;

public sealed class CourseOfferingService : ICourseOfferingService
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CourseOfferingService(
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

    public async Task<CourseOfferingModel> CreateAsync(CreateCourseOfferingCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(command.Code);
        var displayName = NormalizeDisplayName(command.DisplayName);
        var capacity = NormalizeCapacity(command.Capacity);
        var course = await RequireCourseAsync(command.CourseId, cancellationToken);
        var semester = await RequireSemesterAsync(command.SemesterId, cancellationToken);
        EnsureStatus(command.Status);

        var exists = await _dbContext.CourseOfferings
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == normalizedCode, cancellationToken);
        if (exists)
        {
            throw new AuthException("Course offering code already exists.");
        }

        var courseOffering = new CourseOffering
        {
            Code = normalizedCode,
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = displayName,
            Capacity = capacity,
            Status = command.Status,
            Description = NormalizeDescription(command.Description),
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _dbContext.AddAsync(courseOffering, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("courseoffering.create", nameof(CourseOffering), courseOffering.Id.ToString(), new
        {
            courseOffering.Code,
            courseOffering.CourseId,
            courseOffering.SemesterId,
            courseOffering.DisplayName,
            courseOffering.Capacity,
            courseOffering.Status
        }, _currentUser.UserId, cancellationToken);

        return Map(courseOffering, course, semester);
    }

    public async Task<CourseOfferingModel> UpdateAsync(UpdateCourseOfferingCommand command, CancellationToken cancellationToken = default)
    {
        var courseOffering = await _dbContext.CourseOfferings
            .Include(x => x.Course)
            .Include(x => x.Semester)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Course offering was not found.");

        var normalizedCode = NormalizeCode(command.Code);
        var displayName = NormalizeDisplayName(command.DisplayName);
        var capacity = NormalizeCapacity(command.Capacity);
        var course = await RequireCourseAsync(command.CourseId, cancellationToken);
        var semester = await RequireSemesterAsync(command.SemesterId, cancellationToken);
        EnsureStatus(command.Status);

        var duplicateCode = await _dbContext.CourseOfferings
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id != courseOffering.Id && x.Code == normalizedCode, cancellationToken);
        if (duplicateCode)
        {
            throw new AuthException("Course offering code already exists.");
        }

        courseOffering.Code = normalizedCode;
        courseOffering.CourseId = course.Id;
        courseOffering.SemesterId = semester.Id;
        courseOffering.DisplayName = displayName;
        courseOffering.Capacity = capacity;
        courseOffering.Status = command.Status;
        courseOffering.Description = NormalizeDescription(command.Description);
        courseOffering.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("courseoffering.update", nameof(CourseOffering), courseOffering.Id.ToString(), new
        {
            courseOffering.Code,
            courseOffering.CourseId,
            courseOffering.SemesterId,
            courseOffering.DisplayName,
            courseOffering.Capacity,
            courseOffering.Status
        }, _currentUser.UserId, cancellationToken);

        return Map(courseOffering, course, semester);
    }

    public async Task DeleteAsync(DeleteCourseOfferingCommand command, CancellationToken cancellationToken = default)
    {
        var courseOffering = await _dbContext.CourseOfferings.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Course offering was not found.");

        courseOffering.IsDeleted = true;
        courseOffering.Status = CourseOfferingStatus.Inactive;
        courseOffering.DeletedAtUtc = _dateTimeProvider.UtcNow;
        courseOffering.DeletedBy = _currentUser.Username ?? "system";
        courseOffering.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("courseoffering.delete", nameof(CourseOffering), courseOffering.Id.ToString(), new
        {
            courseOffering.Code,
            courseOffering.DisplayName
        }, _currentUser.UserId, cancellationToken);
    }

    public async Task<CourseOfferingModel> GetByIdAsync(GetCourseOfferingByIdQuery query, CancellationToken cancellationToken = default)
    {
        var courseOffering = await _dbContext.CourseOfferings
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Semester)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw new AuthException("Course offering was not found.");

        return Map(courseOffering, courseOffering.Course, courseOffering.Semester);
    }

    public async Task<IReadOnlyCollection<CourseOfferingListItemModel>> GetListAsync(GetCourseOfferingsQuery query, CancellationToken cancellationToken = default)
    {
        var courseOfferings = _dbContext.CourseOfferings
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Semester)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            courseOfferings = courseOfferings.Where(x =>
                x.Code.Contains(keyword) ||
                x.DisplayName.Contains(keyword) ||
                (x.Course != null && x.Course.Name.Contains(keyword)));
        }

        if (query.CourseId.HasValue)
        {
            courseOfferings = courseOfferings.Where(x => x.CourseId == query.CourseId.Value);
        }

        if (query.SemesterId.HasValue)
        {
            courseOfferings = courseOfferings.Where(x => x.SemesterId == query.SemesterId.Value);
        }

        if (query.Status.HasValue)
        {
            courseOfferings = courseOfferings.Where(x => x.Status == query.Status.Value);
        }

        return await courseOfferings
            .OrderBy(x => x.Code)
            .Select(x => new CourseOfferingListItemModel
            {
                Id = x.Id,
                Code = x.Code,
                CourseId = x.CourseId,
                CourseCode = x.Course != null ? x.Course.Code : string.Empty,
                CourseName = x.Course != null ? x.Course.Name : string.Empty,
                SemesterId = x.SemesterId,
                SemesterName = x.Semester != null ? x.Semester.Name : string.Empty,
                DisplayName = x.DisplayName,
                Capacity = x.Capacity,
                Status = x.Status
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<Course> RequireCourseAsync(Guid courseId, CancellationToken cancellationToken)
    {
        if (courseId == Guid.Empty)
        {
            throw new AuthException("Course is required.");
        }

        var course = await _dbContext.Courses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken);

        if (course is null || course.IsDeleted)
        {
            throw new AuthException("Course was not found.");
        }

        return course;
    }

    private async Task<Semester> RequireSemesterAsync(Guid semesterId, CancellationToken cancellationToken)
    {
        if (semesterId == Guid.Empty)
        {
            throw new AuthException("Semester is required.");
        }

        var semester = await _dbContext.Semesters
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == semesterId, cancellationToken);

        if (semester is null || semester.IsDeleted)
        {
            throw new AuthException("Semester was not found.");
        }

        return semester;
    }

    private static CourseOfferingModel Map(CourseOffering courseOffering, Course? course, Semester? semester)
    {
        return new CourseOfferingModel
        {
            Id = courseOffering.Id,
            Code = courseOffering.Code,
            CourseId = courseOffering.CourseId,
            CourseCode = course?.Code ?? string.Empty,
            CourseName = course?.Name ?? string.Empty,
            SemesterId = courseOffering.SemesterId,
            SemesterCode = semester?.Code ?? string.Empty,
            SemesterName = semester?.Name ?? string.Empty,
            DisplayName = courseOffering.DisplayName,
            Capacity = courseOffering.Capacity,
            Status = courseOffering.Status,
            Description = courseOffering.Description
        };
    }

    private static string NormalizeCode(string code)
    {
        var normalized = code.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Course offering code is required.");
        }

        return normalized;
    }

    private static string NormalizeDisplayName(string displayName)
    {
        var normalized = displayName.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Course offering display name is required.");
        }

        return normalized;
    }

    private static int NormalizeCapacity(int capacity)
    {
        if (capacity < 1 || capacity > 500)
        {
            throw new AuthException("Course offering capacity is invalid.");
        }

        return capacity;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }

    private static void EnsureStatus(CourseOfferingStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new AuthException("Course offering status is invalid.");
        }
    }
}
