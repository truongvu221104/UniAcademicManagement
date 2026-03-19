using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Courses;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Courses;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Features.Courses;

public sealed class CourseService : ICourseService
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CourseService(
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

    public async Task<CourseModel> CreateAsync(CreateCourseCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(command.Code);
        var normalizedName = NormalizeName(command.Name);
        var credits = NormalizeCredits(command.Credits);
        var faculty = await ResolveFacultyAsync(command.FacultyId, cancellationToken);
        EnsureStatus(command.Status);

        var exists = await _dbContext.Courses
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == normalizedCode, cancellationToken);
        if (exists)
        {
            throw new AuthException("Course code already exists.");
        }

        var course = new Course
        {
            Code = normalizedCode,
            Name = normalizedName,
            Credits = credits,
            FacultyId = faculty?.Id,
            Status = command.Status,
            Description = NormalizeDescription(command.Description),
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _dbContext.AddAsync(course, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("course.create", nameof(Course), course.Id.ToString(), new
        {
            course.Code,
            course.Name,
            course.Credits,
            course.FacultyId,
            course.Status
        }, _currentUser.UserId, cancellationToken);

        return Map(course, faculty);
    }

    public async Task<CourseModel> UpdateAsync(UpdateCourseCommand command, CancellationToken cancellationToken = default)
    {
        var course = await _dbContext.Courses
            .Include(x => x.Faculty)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Course was not found.");

        var normalizedCode = NormalizeCode(command.Code);
        var normalizedName = NormalizeName(command.Name);
        var credits = NormalizeCredits(command.Credits);
        var faculty = await ResolveFacultyAsync(command.FacultyId, cancellationToken);
        EnsureStatus(command.Status);

        var duplicateCode = await _dbContext.Courses
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id != course.Id && x.Code == normalizedCode, cancellationToken);
        if (duplicateCode)
        {
            throw new AuthException("Course code already exists.");
        }

        course.Code = normalizedCode;
        course.Name = normalizedName;
        course.Credits = credits;
        course.FacultyId = faculty?.Id;
        course.Status = command.Status;
        course.Description = NormalizeDescription(command.Description);
        course.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("course.update", nameof(Course), course.Id.ToString(), new
        {
            course.Code,
            course.Name,
            course.Credits,
            course.FacultyId,
            course.Status
        }, _currentUser.UserId, cancellationToken);

        return Map(course, faculty);
    }

    public async Task DeleteAsync(DeleteCourseCommand command, CancellationToken cancellationToken = default)
    {
        var course = await _dbContext.Courses.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Course was not found.");

        course.IsDeleted = true;
        course.Status = CourseStatus.Inactive;
        course.DeletedAtUtc = _dateTimeProvider.UtcNow;
        course.DeletedBy = _currentUser.Username ?? "system";
        course.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("course.delete", nameof(Course), course.Id.ToString(), new
        {
            course.Code,
            course.Name
        }, _currentUser.UserId, cancellationToken);
    }

    public async Task<CourseModel> GetByIdAsync(GetCourseByIdQuery query, CancellationToken cancellationToken = default)
    {
        var course = await _dbContext.Courses
            .AsNoTracking()
            .Include(x => x.Faculty)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw new AuthException("Course was not found.");

        return Map(course, course.Faculty);
    }

    public async Task<IReadOnlyCollection<CourseListItemModel>> GetListAsync(GetCoursesQuery query, CancellationToken cancellationToken = default)
    {
        var courses = _dbContext.Courses
            .AsNoTracking()
            .Include(x => x.Faculty)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            courses = courses.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword));
        }

        if (query.FacultyId.HasValue)
        {
            courses = courses.Where(x => x.FacultyId == query.FacultyId.Value);
        }

        if (query.Status.HasValue)
        {
            courses = courses.Where(x => x.Status == query.Status.Value);
        }

        return await courses
            .OrderBy(x => x.Code)
            .Select(x => new CourseListItemModel
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Credits = x.Credits,
                FacultyId = x.FacultyId,
                FacultyName = x.Faculty != null ? x.Faculty.Name : string.Empty,
                Status = x.Status
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<Faculty?> ResolveFacultyAsync(Guid? facultyId, CancellationToken cancellationToken)
    {
        if (!facultyId.HasValue)
        {
            return null;
        }

        var faculty = await _dbContext.Faculties
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == facultyId.Value, cancellationToken);

        if (faculty is null || faculty.IsDeleted)
        {
            throw new AuthException("Faculty was not found.");
        }

        return faculty;
    }

    private static CourseModel Map(Course course, Faculty? faculty)
    {
        return new CourseModel
        {
            Id = course.Id,
            Code = course.Code,
            Name = course.Name,
            Credits = course.Credits,
            FacultyId = course.FacultyId,
            FacultyCode = faculty?.Code ?? string.Empty,
            FacultyName = faculty?.Name ?? string.Empty,
            Status = course.Status,
            Description = course.Description
        };
    }

    private static string NormalizeCode(string code)
    {
        var normalized = code.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Course code is required.");
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Course name is required.");
        }

        return normalized;
    }

    private static int NormalizeCredits(int credits)
    {
        if (credits < 1 || credits > 15)
        {
            throw new AuthException("Course credits are invalid.");
        }

        return credits;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }

    private static void EnsureStatus(CourseStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new AuthException("Course status is invalid.");
        }
    }
}
