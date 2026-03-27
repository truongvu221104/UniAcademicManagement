using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.LecturerProfiles;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.LecturerProfiles;
using UniAcademic.Domain.Entities.Academic;

namespace UniAcademic.Application.Features.LecturerProfiles;

public sealed class LecturerProfileService : ILecturerProfileService
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LecturerProfileService(
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

    public async Task<LecturerProfileModel> CreateAsync(CreateLecturerProfileCommand command, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(command.Code);
        var fullName = NormalizeFullName(command.FullName);
        var faculty = await RequireFacultyAsync(command.FacultyId, cancellationToken);
        var email = NormalizeEmail(command.Email);
        var phoneNumber = NormalizePhoneNumber(command.PhoneNumber);

        var duplicateCode = await _dbContext.LecturerProfiles
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == code, cancellationToken);
        if (duplicateCode)
        {
            throw new AuthException("Lecturer profile code already exists.");
        }

        var lecturerProfile = new LecturerProfile
        {
            Code = code,
            FullName = fullName,
            Email = email,
            PhoneNumber = phoneNumber,
            FacultyId = faculty.Id,
            IsActive = command.IsActive,
            Note = NormalizeNote(command.Note),
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _dbContext.AddAsync(lecturerProfile, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("lecturerprofile.create", nameof(LecturerProfile), lecturerProfile.Id.ToString(), new
        {
            lecturerProfile.Code,
            lecturerProfile.FullName,
            lecturerProfile.FacultyId,
            lecturerProfile.IsActive
        }, _currentUser.UserId, cancellationToken);

        return Map(lecturerProfile, faculty);
    }

    public async Task<LecturerProfileModel> UpdateAsync(UpdateLecturerProfileCommand command, CancellationToken cancellationToken = default)
    {
        var lecturerProfile = await _dbContext.LecturerProfiles
            .Include(x => x.Faculty)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Lecturer profile was not found.");

        var code = NormalizeCode(command.Code);
        var fullName = NormalizeFullName(command.FullName);
        var faculty = await RequireFacultyAsync(command.FacultyId, cancellationToken);
        var email = NormalizeEmail(command.Email);
        var phoneNumber = NormalizePhoneNumber(command.PhoneNumber);

        var duplicateCode = await _dbContext.LecturerProfiles
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id != lecturerProfile.Id && x.Code == code, cancellationToken);
        if (duplicateCode)
        {
            throw new AuthException("Lecturer profile code already exists.");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedUserEmail = email.ToUpperInvariant();
            var duplicateUserEmail = await _dbContext.Users
                .AnyAsync(
                    x => x.LecturerProfileId != lecturerProfile.Id
                         && x.NormalizedEmail == normalizedUserEmail,
                    cancellationToken);
            if (duplicateUserEmail)
            {
                throw new AuthException("Email address is already used by another account.");
            }
        }

        lecturerProfile.Code = code;
        lecturerProfile.FullName = fullName;
        lecturerProfile.Email = email;
        lecturerProfile.PhoneNumber = phoneNumber;
        lecturerProfile.FacultyId = faculty.Id;
        lecturerProfile.IsActive = command.IsActive;
        lecturerProfile.Note = NormalizeNote(command.Note);
        lecturerProfile.ModifiedBy = _currentUser.Username ?? "system";

        if (!string.IsNullOrWhiteSpace(email))
        {
            var linkedUser = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.LecturerProfileId == lecturerProfile.Id, cancellationToken);
            if (linkedUser is not null)
            {
                linkedUser.Email = email;
                linkedUser.NormalizedEmail = email.ToUpperInvariant();
                linkedUser.ModifiedBy = _currentUser.Username ?? "system";
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("lecturerprofile.update", nameof(LecturerProfile), lecturerProfile.Id.ToString(), new
        {
            lecturerProfile.Code,
            lecturerProfile.FullName,
            lecturerProfile.FacultyId,
            lecturerProfile.IsActive
        }, _currentUser.UserId, cancellationToken);

        return Map(lecturerProfile, faculty);
    }

    public async Task DeleteAsync(DeleteLecturerProfileCommand command, CancellationToken cancellationToken = default)
    {
        var lecturerProfile = await _dbContext.LecturerProfiles
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Lecturer profile was not found.");

        var hasAssignments = await _dbContext.LecturerAssignments
            .AnyAsync(x => x.LecturerProfileId == lecturerProfile.Id, cancellationToken);
        if (hasAssignments)
        {
            throw new AuthException("Lecturer profile still has assignments.");
        }

        lecturerProfile.IsDeleted = true;
        lecturerProfile.IsActive = false;
        lecturerProfile.DeletedAtUtc = _dateTimeProvider.UtcNow;
        lecturerProfile.DeletedBy = _currentUser.Username ?? "system";
        lecturerProfile.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("lecturerprofile.delete", nameof(LecturerProfile), lecturerProfile.Id.ToString(), new
        {
            lecturerProfile.Code,
            lecturerProfile.FullName
        }, _currentUser.UserId, cancellationToken);
    }

    public async Task<LecturerProfileModel> GetByIdAsync(GetLecturerProfileByIdQuery query, CancellationToken cancellationToken = default)
    {
        var lecturerProfile = await _dbContext.LecturerProfiles
            .AsNoTracking()
            .Include(x => x.Faculty)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw new AuthException("Lecturer profile was not found.");

        return Map(lecturerProfile, lecturerProfile.Faculty);
    }

    public async Task<IReadOnlyCollection<LecturerProfileListItemModel>> GetListAsync(GetLecturerProfilesQuery query, CancellationToken cancellationToken = default)
    {
        var lecturerProfiles = _dbContext.LecturerProfiles
            .AsNoTracking()
            .Include(x => x.Faculty)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            lecturerProfiles = lecturerProfiles.Where(x =>
                x.Code.Contains(keyword) ||
                x.FullName.Contains(keyword) ||
                (x.Email != null && x.Email.Contains(keyword)));
        }

        if (query.FacultyId.HasValue)
        {
            lecturerProfiles = lecturerProfiles.Where(x => x.FacultyId == query.FacultyId.Value);
        }

        if (query.IsActive.HasValue)
        {
            lecturerProfiles = lecturerProfiles.Where(x => x.IsActive == query.IsActive.Value);
        }

        return await lecturerProfiles
            .OrderBy(x => x.Code)
            .Select(x => new LecturerProfileListItemModel
            {
                Id = x.Id,
                Code = x.Code,
                FullName = x.FullName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                FacultyId = x.FacultyId,
                FacultyName = x.Faculty != null ? x.Faculty.Name : string.Empty,
                IsActive = x.IsActive
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

    private static LecturerProfileModel Map(LecturerProfile lecturerProfile, Faculty? faculty)
    {
        return new LecturerProfileModel
        {
            Id = lecturerProfile.Id,
            Code = lecturerProfile.Code,
            FullName = lecturerProfile.FullName,
            Email = lecturerProfile.Email,
            PhoneNumber = lecturerProfile.PhoneNumber,
            FacultyId = lecturerProfile.FacultyId,
            FacultyCode = faculty?.Code ?? string.Empty,
            FacultyName = faculty?.Name ?? string.Empty,
            IsActive = lecturerProfile.IsActive,
            Note = lecturerProfile.Note
        };
    }

    private static string NormalizeCode(string code)
    {
        var normalized = code.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Lecturer profile code is required.");
        }

        return normalized;
    }

    private static string NormalizeFullName(string fullName)
    {
        var normalized = fullName.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Lecturer profile full name is required.");
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
            throw new AuthException("Lecturer profile email is invalid.");
        }

        return normalized;
    }

    private static string? NormalizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var normalized = phoneNumber.Trim();
        if (normalized.Length < 6 || normalized.Length > 20)
        {
            throw new AuthException("Lecturer profile phone number is invalid.");
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
            throw new AuthException("Lecturer profile note is invalid.");
        }

        return normalized;
    }
}
