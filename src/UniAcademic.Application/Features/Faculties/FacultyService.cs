using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Faculties;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Faculties;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Features.Faculties;

public sealed class FacultyService : IFacultyService
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public FacultyService(
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

    public async Task<FacultyModel> CreateAsync(CreateFacultyCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(command.Code);
        var normalizedName = NormalizeName(command.Name);
        EnsureStatus(command.Status);

        var exists = await _dbContext.Faculties
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == normalizedCode, cancellationToken);
        if (exists)
        {
            throw new AuthException("Faculty code already exists.");
        }

        var faculty = new Faculty
        {
            Code = normalizedCode,
            Name = normalizedName,
            Description = NormalizeDescription(command.Description),
            Status = command.Status,
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _dbContext.AddAsync(faculty, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("faculty.create", nameof(Faculty), faculty.Id.ToString(), new
        {
            faculty.Code,
            faculty.Name,
            faculty.Status
        }, _currentUser.UserId, cancellationToken);

        return Map(faculty);
    }

    public async Task<FacultyModel> UpdateAsync(UpdateFacultyCommand command, CancellationToken cancellationToken = default)
    {
        var faculty = await _dbContext.Faculties.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Faculty was not found.");

        var normalizedCode = NormalizeCode(command.Code);
        var normalizedName = NormalizeName(command.Name);
        EnsureStatus(command.Status);

        var duplicateCode = await _dbContext.Faculties
            .IgnoreQueryFilters()
            .AnyAsync(
                x => x.Id != faculty.Id && x.Code == normalizedCode,
                cancellationToken);

        if (duplicateCode)
        {
            throw new AuthException("Faculty code already exists.");
        }

        faculty.Code = normalizedCode;
        faculty.Name = normalizedName;
        faculty.Description = NormalizeDescription(command.Description);
        faculty.Status = command.Status;
        faculty.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("faculty.update", nameof(Faculty), faculty.Id.ToString(), new
        {
            faculty.Code,
            faculty.Name,
            faculty.Status
        }, _currentUser.UserId, cancellationToken);

        return Map(faculty);
    }

    public async Task DeleteAsync(DeleteFacultyCommand command, CancellationToken cancellationToken = default)
    {
        var faculty = await _dbContext.Faculties.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Faculty was not found.");

        faculty.IsDeleted = true;
        faculty.Status = FacultyStatus.Inactive;
        faculty.DeletedAtUtc = _dateTimeProvider.UtcNow;
        faculty.DeletedBy = _currentUser.Username ?? "system";
        faculty.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("faculty.delete", nameof(Faculty), faculty.Id.ToString(), new
        {
            faculty.Code,
            faculty.Name
        }, _currentUser.UserId, cancellationToken);
    }

    public async Task<FacultyModel> GetByIdAsync(GetFacultyByIdQuery query, CancellationToken cancellationToken = default)
    {
        var faculty = await _dbContext.Faculties
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw new AuthException("Faculty was not found.");

        return Map(faculty);
    }

    public async Task<IReadOnlyCollection<FacultyListItemModel>> GetListAsync(GetFacultiesQuery query, CancellationToken cancellationToken = default)
    {
        var faculties = _dbContext.Faculties.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            faculties = faculties.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword));
        }

        if (query.Status.HasValue)
        {
            faculties = faculties.Where(x => x.Status == query.Status.Value);
        }

        return await faculties
            .OrderBy(x => x.Code)
            .Select(x => new FacultyListItemModel
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Status = x.Status
            })
            .ToListAsync(cancellationToken);
    }

    private static FacultyModel Map(Faculty faculty)
    {
        return new FacultyModel
        {
            Id = faculty.Id,
            Code = faculty.Code,
            Name = faculty.Name,
            Description = faculty.Description,
            Status = faculty.Status
        };
    }

    private static string NormalizeCode(string code)
    {
        var normalized = code.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Faculty code is required.");
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Faculty name is required.");
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }

    private static void EnsureStatus(FacultyStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new AuthException("Faculty status is invalid.");
        }
    }
}
