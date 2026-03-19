using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Abstractions.StudentClasses;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.StudentClasses;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Features.StudentClasses;

public sealed class StudentClassService : IStudentClassService
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public StudentClassService(
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

    public async Task<StudentClassModel> CreateAsync(CreateStudentClassCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(command.Code);
        var normalizedName = NormalizeName(command.Name);
        var intakeYear = NormalizeIntakeYear(command.IntakeYear, _dateTimeProvider.UtcNow.Year);
        var faculty = await RequireFacultyAsync(command.FacultyId, cancellationToken);
        EnsureStatus(command.Status);

        var exists = await _dbContext.StudentClasses
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == normalizedCode, cancellationToken);
        if (exists)
        {
            throw new AuthException("Student class code already exists.");
        }

        var studentClass = new StudentClass
        {
            Code = normalizedCode,
            Name = normalizedName,
            FacultyId = faculty.Id,
            IntakeYear = intakeYear,
            Status = command.Status,
            Description = NormalizeDescription(command.Description),
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _dbContext.AddAsync(studentClass, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("studentclass.create", nameof(StudentClass), studentClass.Id.ToString(), new
        {
            studentClass.Code,
            studentClass.Name,
            studentClass.FacultyId,
            studentClass.IntakeYear,
            studentClass.Status
        }, _currentUser.UserId, cancellationToken);

        return Map(studentClass, faculty);
    }

    public async Task<StudentClassModel> UpdateAsync(UpdateStudentClassCommand command, CancellationToken cancellationToken = default)
    {
        var studentClass = await _dbContext.StudentClasses
            .Include(x => x.Faculty)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Student class was not found.");

        var normalizedCode = NormalizeCode(command.Code);
        var normalizedName = NormalizeName(command.Name);
        var intakeYear = NormalizeIntakeYear(command.IntakeYear, _dateTimeProvider.UtcNow.Year);
        var faculty = await RequireFacultyAsync(command.FacultyId, cancellationToken);
        EnsureStatus(command.Status);

        var duplicateCode = await _dbContext.StudentClasses
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id != studentClass.Id && x.Code == normalizedCode, cancellationToken);

        if (duplicateCode)
        {
            throw new AuthException("Student class code already exists.");
        }

        studentClass.Code = normalizedCode;
        studentClass.Name = normalizedName;
        studentClass.FacultyId = faculty.Id;
        studentClass.IntakeYear = intakeYear;
        studentClass.Status = command.Status;
        studentClass.Description = NormalizeDescription(command.Description);
        studentClass.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("studentclass.update", nameof(StudentClass), studentClass.Id.ToString(), new
        {
            studentClass.Code,
            studentClass.Name,
            studentClass.FacultyId,
            studentClass.IntakeYear,
            studentClass.Status
        }, _currentUser.UserId, cancellationToken);

        return Map(studentClass, faculty);
    }

    public async Task DeleteAsync(DeleteStudentClassCommand command, CancellationToken cancellationToken = default)
    {
        var studentClass = await _dbContext.StudentClasses.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Student class was not found.");

        studentClass.IsDeleted = true;
        studentClass.Status = StudentClassStatus.Inactive;
        studentClass.DeletedAtUtc = _dateTimeProvider.UtcNow;
        studentClass.DeletedBy = _currentUser.Username ?? "system";
        studentClass.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("studentclass.delete", nameof(StudentClass), studentClass.Id.ToString(), new
        {
            studentClass.Code,
            studentClass.Name
        }, _currentUser.UserId, cancellationToken);
    }

    public async Task<StudentClassModel> GetByIdAsync(GetStudentClassByIdQuery query, CancellationToken cancellationToken = default)
    {
        var studentClass = await _dbContext.StudentClasses
            .AsNoTracking()
            .Include(x => x.Faculty)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw new AuthException("Student class was not found.");

        return Map(studentClass, studentClass.Faculty);
    }

    public async Task<IReadOnlyCollection<StudentClassListItemModel>> GetListAsync(GetStudentClassesQuery query, CancellationToken cancellationToken = default)
    {
        var studentClasses = _dbContext.StudentClasses
            .AsNoTracking()
            .Include(x => x.Faculty)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            studentClasses = studentClasses.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword));
        }

        if (query.FacultyId.HasValue)
        {
            studentClasses = studentClasses.Where(x => x.FacultyId == query.FacultyId.Value);
        }

        if (query.IntakeYear.HasValue)
        {
            studentClasses = studentClasses.Where(x => x.IntakeYear == query.IntakeYear.Value);
        }

        if (query.Status.HasValue)
        {
            studentClasses = studentClasses.Where(x => x.Status == query.Status.Value);
        }

        return await studentClasses
            .OrderBy(x => x.Code)
            .Select(x => new StudentClassListItemModel
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                FacultyId = x.FacultyId,
                FacultyName = x.Faculty != null ? x.Faculty.Name : string.Empty,
                IntakeYear = x.IntakeYear,
                Status = x.Status
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<Faculty> RequireFacultyAsync(Guid facultyId, CancellationToken cancellationToken)
    {
        if (facultyId == Guid.Empty)
        {
            throw new AuthException("Faculty is required.");
        }

        var faculty = await _dbContext.Faculties
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == facultyId, cancellationToken);

        if (faculty is null || faculty.IsDeleted)
        {
            throw new AuthException("Faculty was not found.");
        }

        return faculty;
    }

    private static StudentClassModel Map(StudentClass studentClass, Faculty? faculty)
    {
        return new StudentClassModel
        {
            Id = studentClass.Id,
            Code = studentClass.Code,
            Name = studentClass.Name,
            FacultyId = studentClass.FacultyId,
            FacultyCode = faculty?.Code ?? string.Empty,
            FacultyName = faculty?.Name ?? string.Empty,
            IntakeYear = studentClass.IntakeYear,
            Status = studentClass.Status,
            Description = studentClass.Description
        };
    }

    private static string NormalizeCode(string code)
    {
        var normalized = code.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Student class code is required.");
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Student class name is required.");
        }

        return normalized;
    }

    private static int NormalizeIntakeYear(int intakeYear, int currentYear)
    {
        if (intakeYear < 2000 || intakeYear > currentYear + 1)
        {
            throw new AuthException("Student class intake year is invalid.");
        }

        return intakeYear;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }

    private static void EnsureStatus(StudentClassStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new AuthException("Student class status is invalid.");
        }
    }
}
