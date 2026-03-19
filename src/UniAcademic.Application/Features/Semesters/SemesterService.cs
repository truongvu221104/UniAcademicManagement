using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Abstractions.Semesters;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Semesters;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Features.Semesters;

public sealed partial class SemesterService : ISemesterService
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SemesterService(
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

    public async Task<SemesterModel> CreateAsync(CreateSemesterCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(command.Code);
        var normalizedName = NormalizeName(command.Name);
        var academicYear = NormalizeAcademicYear(command.AcademicYear);
        var termNo = NormalizeTermNo(command.TermNo);
        var (startDate, endDate) = NormalizeDateRange(command.StartDate, command.EndDate);
        EnsureStatus(command.Status);

        var exists = await _dbContext.Semesters
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == normalizedCode, cancellationToken);
        if (exists)
        {
            throw new AuthException("Semester code already exists.");
        }

        var semester = new Semester
        {
            Code = normalizedCode,
            Name = normalizedName,
            AcademicYear = academicYear,
            TermNo = termNo,
            StartDate = startDate,
            EndDate = endDate,
            Status = command.Status,
            Description = NormalizeDescription(command.Description),
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _dbContext.AddAsync(semester, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("semester.create", nameof(Semester), semester.Id.ToString(), new
        {
            semester.Code,
            semester.Name,
            semester.AcademicYear,
            semester.TermNo,
            semester.StartDate,
            semester.EndDate,
            semester.Status
        }, _currentUser.UserId, cancellationToken);

        return Map(semester);
    }

    public async Task<SemesterModel> UpdateAsync(UpdateSemesterCommand command, CancellationToken cancellationToken = default)
    {
        var semester = await _dbContext.Semesters
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Semester was not found.");

        var normalizedCode = NormalizeCode(command.Code);
        var normalizedName = NormalizeName(command.Name);
        var academicYear = NormalizeAcademicYear(command.AcademicYear);
        var termNo = NormalizeTermNo(command.TermNo);
        var (startDate, endDate) = NormalizeDateRange(command.StartDate, command.EndDate);
        EnsureStatus(command.Status);

        var duplicateCode = await _dbContext.Semesters
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id != semester.Id && x.Code == normalizedCode, cancellationToken);
        if (duplicateCode)
        {
            throw new AuthException("Semester code already exists.");
        }

        semester.Code = normalizedCode;
        semester.Name = normalizedName;
        semester.AcademicYear = academicYear;
        semester.TermNo = termNo;
        semester.StartDate = startDate;
        semester.EndDate = endDate;
        semester.Status = command.Status;
        semester.Description = NormalizeDescription(command.Description);
        semester.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("semester.update", nameof(Semester), semester.Id.ToString(), new
        {
            semester.Code,
            semester.Name,
            semester.AcademicYear,
            semester.TermNo,
            semester.StartDate,
            semester.EndDate,
            semester.Status
        }, _currentUser.UserId, cancellationToken);

        return Map(semester);
    }

    public async Task DeleteAsync(DeleteSemesterCommand command, CancellationToken cancellationToken = default)
    {
        var semester = await _dbContext.Semesters.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException("Semester was not found.");

        semester.IsDeleted = true;
        semester.Status = SemesterStatus.Inactive;
        semester.DeletedAtUtc = _dateTimeProvider.UtcNow;
        semester.DeletedBy = _currentUser.Username ?? "system";
        semester.ModifiedBy = _currentUser.Username ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("semester.delete", nameof(Semester), semester.Id.ToString(), new
        {
            semester.Code,
            semester.Name
        }, _currentUser.UserId, cancellationToken);
    }

    public async Task<SemesterModel> GetByIdAsync(GetSemesterByIdQuery query, CancellationToken cancellationToken = default)
    {
        var semester = await _dbContext.Semesters
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw new AuthException("Semester was not found.");

        return Map(semester);
    }

    public async Task<IReadOnlyCollection<SemesterListItemModel>> GetListAsync(GetSemestersQuery query, CancellationToken cancellationToken = default)
    {
        var semesters = _dbContext.Semesters.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            semesters = semesters.Where(x =>
                x.Code.Contains(keyword) ||
                x.Name.Contains(keyword) ||
                x.AcademicYear.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(query.AcademicYear))
        {
            var academicYear = query.AcademicYear.Trim();
            semesters = semesters.Where(x => x.AcademicYear == academicYear);
        }

        if (query.TermNo.HasValue)
        {
            semesters = semesters.Where(x => x.TermNo == query.TermNo.Value);
        }

        if (query.Status.HasValue)
        {
            semesters = semesters.Where(x => x.Status == query.Status.Value);
        }

        return await semesters
            .OrderBy(x => x.AcademicYear)
            .ThenBy(x => x.TermNo)
            .Select(x => new SemesterListItemModel
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                AcademicYear = x.AcademicYear,
                TermNo = x.TermNo,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status
            })
            .ToListAsync(cancellationToken);
    }

    private static SemesterModel Map(Semester semester)
    {
        return new SemesterModel
        {
            Id = semester.Id,
            Code = semester.Code,
            Name = semester.Name,
            AcademicYear = semester.AcademicYear,
            TermNo = semester.TermNo,
            StartDate = semester.StartDate,
            EndDate = semester.EndDate,
            Status = semester.Status,
            Description = semester.Description
        };
    }

    private static string NormalizeCode(string code)
    {
        var normalized = code.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Semester code is required.");
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Semester name is required.");
        }

        return normalized;
    }

    private static string NormalizeAcademicYear(string academicYear)
    {
        var normalized = academicYear.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new AuthException("Semester academic year is required.");
        }

        var match = AcademicYearRegex().Match(normalized);
        if (!match.Success)
        {
            throw new AuthException("Semester academic year is invalid.");
        }

        var startYear = int.Parse(match.Groups[1].Value);
        var endYear = int.Parse(match.Groups[2].Value);
        if (endYear != startYear + 1)
        {
            throw new AuthException("Semester academic year is invalid.");
        }

        return normalized;
    }

    private static int NormalizeTermNo(int termNo)
    {
        if (termNo < 1 || termNo > 3)
        {
            throw new AuthException("Semester term number is invalid.");
        }

        return termNo;
    }

    private static (DateTime StartDate, DateTime EndDate) NormalizeDateRange(DateTime startDate, DateTime endDate)
    {
        var normalizedStartDate = startDate.Date;
        var normalizedEndDate = endDate.Date;

        if (normalizedStartDate > normalizedEndDate)
        {
            throw new AuthException("Semester date range is invalid.");
        }

        return (normalizedStartDate, normalizedEndDate);
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }

    private static void EnsureStatus(SemesterStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new AuthException("Semester status is invalid.");
        }
    }

    [GeneratedRegex(@"^(\d{4})-(\d{4})$")]
    private static partial Regex AcademicYearRegex();
}
