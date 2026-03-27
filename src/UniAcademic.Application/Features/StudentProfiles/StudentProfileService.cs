using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Abstractions.StudentProfiles;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.StudentProfiles;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Features.StudentProfiles;

public sealed class StudentProfileService : IStudentProfileService
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public StudentProfileService(
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

    public async Task<StudentProfileModel> CreateAsync(CreateStudentProfileCommand command, CancellationToken cancellationToken = default)
    {
        var studentCode = NormalizeStudentCode(command.Code);
        var fullName = NormalizeFullName(command.FullName);
        var studentClass = await RequireStudentClassAsync(command.StudentClassId, cancellationToken);
        var email = NormalizeEmail(command.Email);
        var phone = NormalizePhone(command.Phone);
        var dateOfBirth = NormalizeDateOfBirth(command.DateOfBirth, _dateTimeProvider.UtcNow.Date);
        var gender = NormalizeGender(command.Gender);
        var address = NormalizeAddress(command.Address);
        EnsureStatus(command.Status);

        var exists = await _dbContext.StudentProfiles
            .IgnoreQueryFilters()
            .AnyAsync(x => x.StudentCode == studentCode, cancellationToken);
        if (exists)
        {
            throw new AuthException("Student profile code already exists.");
        }

        var studentProfile = new StudentProfile
        {
            StudentCode = studentCode,
            FullName = fullName,
            StudentClassId = studentClass.Id,
            Email = email,
            Phone = phone,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            Address = address,
            Status = command.Status,
            Note = NormalizeNote(command.Note),
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _dbContext.AddAsync(studentProfile, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("studentprofile.create", nameof(StudentProfile), studentProfile.Id.ToString(), new
        {
            studentProfile.StudentCode,
            studentProfile.FullName,
            studentProfile.StudentClassId,
            studentProfile.Status
        }, _currentUser.UserId, cancellationToken);

        return Map(studentProfile, studentClass);
    }

    public async Task<StudentProfileModel> UpdateAsync(UpdateStudentProfileCommand command, CancellationToken cancellationToken = default)
    {
        var studentProfile = await _dbContext.StudentProfiles
            .Include(x => x.StudentClass)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Student profile was not found.");

        var studentCode = NormalizeStudentCode(command.Code);
        var fullName = NormalizeFullName(command.FullName);
        var studentClass = await RequireStudentClassAsync(command.StudentClassId, cancellationToken);
        var email = NormalizeEmail(command.Email);
        var phone = NormalizePhone(command.Phone);
        var dateOfBirth = NormalizeDateOfBirth(command.DateOfBirth, _dateTimeProvider.UtcNow.Date);
        var gender = NormalizeGender(command.Gender);
        var address = NormalizeAddress(command.Address);
        EnsureStatus(command.Status);

        var duplicateCode = await _dbContext.StudentProfiles
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id != studentProfile.Id && x.StudentCode == studentCode, cancellationToken);
        if (duplicateCode)
        {
            throw new AuthException("Student profile code already exists.");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedUserEmail = email.ToUpperInvariant();
            var duplicateUserEmail = await _dbContext.Users
                .AnyAsync(
                    x => x.StudentProfileId != studentProfile.Id
                         && x.NormalizedEmail == normalizedUserEmail,
                    cancellationToken);
            if (duplicateUserEmail)
            {
                throw new AuthException("Email address is already used by another account.");
            }
        }

        studentProfile.StudentCode = studentCode;
        studentProfile.FullName = fullName;
        studentProfile.StudentClassId = studentClass.Id;
        studentProfile.Email = email;
        studentProfile.Phone = phone;
        studentProfile.DateOfBirth = dateOfBirth;
        studentProfile.Gender = gender;
        studentProfile.Address = address;
        studentProfile.Status = command.Status;
        studentProfile.Note = NormalizeNote(command.Note);
        studentProfile.ModifiedBy = _currentUser.Username ?? "system";

        if (!string.IsNullOrWhiteSpace(email))
        {
            var linkedUser = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.StudentProfileId == studentProfile.Id, cancellationToken);
            if (linkedUser is not null)
            {
                linkedUser.Email = email;
                linkedUser.NormalizedEmail = email.ToUpperInvariant();
                linkedUser.ModifiedBy = _currentUser.Username ?? "system";
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("studentprofile.update", nameof(StudentProfile), studentProfile.Id.ToString(), new
        {
            studentProfile.StudentCode,
            studentProfile.FullName,
            studentProfile.StudentClassId,
            studentProfile.Status
        }, _currentUser.UserId, cancellationToken);

        return Map(studentProfile, studentClass);
    }

    public async Task DeleteAsync(DeleteStudentProfileCommand command, CancellationToken cancellationToken = default)
    {
        var studentProfile = await _dbContext.StudentProfiles.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Student profile was not found.");

        studentProfile.IsDeleted = true;
        studentProfile.Status = StudentProfileStatus.Inactive;
        studentProfile.DeletedAtUtc = _dateTimeProvider.UtcNow;
        studentProfile.DeletedBy = _currentUser.Username ?? "system";
        studentProfile.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("studentprofile.delete", nameof(StudentProfile), studentProfile.Id.ToString(), new
        {
            studentProfile.StudentCode,
            studentProfile.FullName
        }, _currentUser.UserId, cancellationToken);
    }

    public async Task<StudentProfileModel> GetByIdAsync(GetStudentProfileByIdQuery query, CancellationToken cancellationToken = default)
    {
        var studentProfile = await _dbContext.StudentProfiles
            .AsNoTracking()
            .Include(x => x.StudentClass)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw new AuthException("Student profile was not found.");

        return Map(studentProfile, studentProfile.StudentClass);
    }

    public async Task<IReadOnlyCollection<StudentProfileListItemModel>> GetListAsync(GetStudentProfilesQuery query, CancellationToken cancellationToken = default)
    {
        var studentProfiles = _dbContext.StudentProfiles
            .AsNoTracking()
            .Include(x => x.StudentClass)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            studentProfiles = studentProfiles.Where(x =>
                x.StudentCode.Contains(keyword) ||
                x.FullName.Contains(keyword) ||
                (x.Email != null && x.Email.Contains(keyword)));
        }

        if (query.StudentClassId.HasValue)
        {
            studentProfiles = studentProfiles.Where(x => x.StudentClassId == query.StudentClassId.Value);
        }

        if (query.Gender.HasValue)
        {
            studentProfiles = studentProfiles.Where(x => x.Gender == query.Gender.Value);
        }

        if (query.Status.HasValue)
        {
            studentProfiles = studentProfiles.Where(x => x.Status == query.Status.Value);
        }

        return await studentProfiles
            .OrderBy(x => x.StudentCode)
            .Select(x => new StudentProfileListItemModel
            {
                Id = x.Id,
                StudentCode = x.StudentCode,
                FullName = x.FullName,
                StudentClassId = x.StudentClassId,
                StudentClassName = x.StudentClass != null ? x.StudentClass.Name : string.Empty,
                Email = x.Email,
                Phone = x.Phone,
                DateOfBirth = x.DateOfBirth,
                Gender = x.Gender,
                Status = x.Status
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<StudentClass> RequireStudentClassAsync(Guid studentClassId, CancellationToken cancellationToken)
    {
        if (studentClassId == Guid.Empty)
        {
            throw new AuthException("Student class is required.");
        }

        var studentClass = await _dbContext.StudentClasses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == studentClassId, cancellationToken);

        if (studentClass is null || studentClass.IsDeleted)
        {
            throw new AuthException("Student class was not found.");
        }

        return studentClass;
    }

    private static StudentProfileModel Map(StudentProfile studentProfile, StudentClass? studentClass)
    {
        return new StudentProfileModel
        {
            Id = studentProfile.Id,
            StudentCode = studentProfile.StudentCode,
            FullName = studentProfile.FullName,
            StudentClassId = studentProfile.StudentClassId,
            StudentClassCode = studentClass?.Code ?? string.Empty,
            StudentClassName = studentClass?.Name ?? string.Empty,
            Email = studentProfile.Email,
            Phone = studentProfile.Phone,
            DateOfBirth = studentProfile.DateOfBirth,
            Gender = studentProfile.Gender,
            Address = studentProfile.Address,
            Status = studentProfile.Status,
            Note = studentProfile.Note
        };
    }

    private static string NormalizeStudentCode(string code)
    {
        var normalized = code.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Student profile code is required.");
        }

        return normalized;
    }

    private static string NormalizeFullName(string fullName)
    {
        var normalized = fullName.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Student profile full name is required.");
        }

        return normalized;
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = email.Trim();
        if (!EmailValidator.IsValid(normalized))
        {
            throw new AuthException("Student profile email is invalid.");
        }

        return normalized;
    }

    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var normalized = phone.Trim();
        if (normalized.Length < 6 || normalized.Length > 20)
        {
            throw new AuthException("Student profile phone is invalid.");
        }

        return normalized;
    }

    private static DateTime? NormalizeDateOfBirth(DateTime? dateOfBirth, DateTime todayUtcDate)
    {
        if (!dateOfBirth.HasValue)
        {
            return null;
        }

        var normalized = dateOfBirth.Value.Date;
        if (normalized > todayUtcDate)
        {
            throw new AuthException("Student profile date of birth is invalid.");
        }

        return normalized;
    }

    private static StudentGender NormalizeGender(StudentGender gender)
    {
        if (!Enum.IsDefined(gender))
        {
            throw new AuthException("Student profile gender is invalid.");
        }

        return gender;
    }

    private static string? NormalizeAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        return address.Trim();
    }

    private static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        return note.Trim();
    }

    private static void EnsureStatus(StudentProfileStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new AuthException("Student profile status is invalid.");
        }
    }
}
