using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Attendance;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Grades;
using UniAcademic.Application.Abstractions.GradeResults;
using UniAcademic.Application.Abstractions.LecturerPortal;
using UniAcademic.Application.Abstractions.Materials;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Attendance;
using UniAcademic.Application.Models.CourseOfferings;
using UniAcademic.Application.Models.Grades;
using UniAcademic.Application.Models.GradeResults;
using UniAcademic.Application.Models.LecturerPortal;
using UniAcademic.Application.Models.Materials;

namespace UniAcademic.Application.Features.LecturerPortal;

public sealed class LecturerPortalService : ILecturerPortalService
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentLecturerContext _currentLecturerContext;
    private readonly IAttendanceService _attendanceService;
    private readonly IGradeService _gradeService;
    private readonly ICourseMaterialService _courseMaterialService;
    private readonly IGradeResultService _gradeResultService;

    public LecturerPortalService(
        IAppDbContext dbContext,
        ICurrentLecturerContext currentLecturerContext,
        IAttendanceService attendanceService,
        IGradeService gradeService,
        ICourseMaterialService courseMaterialService,
        IGradeResultService gradeResultService)
    {
        _dbContext = dbContext;
        _currentLecturerContext = currentLecturerContext;
        _attendanceService = attendanceService;
        _gradeService = gradeService;
        _courseMaterialService = courseMaterialService;
        _gradeResultService = gradeResultService;
    }

    public async Task<IReadOnlyCollection<CourseOfferingListItemModel>> GetMyTeachingOfferingsAsync(GetMyTeachingOfferingsQuery query, CancellationToken cancellationToken = default)
    {
        var lecturerProfileId = await _currentLecturerContext.GetRequiredLecturerProfileIdAsync(cancellationToken);
        var assignments = _dbContext.LecturerAssignments
            .AsNoTracking()
            .Where(x => x.LecturerProfileId == lecturerProfileId)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            assignments = assignments.Where(x =>
                x.CourseOffering != null &&
                (x.CourseOffering.Code.Contains(keyword)
                    || x.CourseOffering.DisplayName.Contains(keyword)
                    || (x.CourseOffering.Course != null && x.CourseOffering.Course.Name.Contains(keyword))));
        }

        return await assignments
            .OrderBy(x => x.CourseOffering!.Code)
            .Select(x => new CourseOfferingListItemModel
            {
                Id = x.CourseOfferingId,
                Code = x.CourseOffering != null ? x.CourseOffering.Code : string.Empty,
                CourseId = x.CourseOffering != null ? x.CourseOffering.CourseId : Guid.Empty,
                CourseCode = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Code : string.Empty,
                CourseName = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Name : string.Empty,
                SemesterId = x.CourseOffering != null ? x.CourseOffering.SemesterId : Guid.Empty,
                SemesterName = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.Name : string.Empty,
                DisplayName = x.CourseOffering != null ? x.CourseOffering.DisplayName : string.Empty,
                Capacity = x.CourseOffering != null ? x.CourseOffering.Capacity : 0,
                Status = x.CourseOffering != null ? x.CourseOffering.Status : default
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AttendanceSessionListItemModel>> GetAttendanceSessionsAsync(GetLecturerAttendanceSessionsQuery query, CancellationToken cancellationToken = default)
    {
        var lecturerProfileId = await _currentLecturerContext.GetRequiredLecturerProfileIdAsync(cancellationToken);
        var sessions = _dbContext.AttendanceSessions
            .AsNoTracking()
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.Records)
            .Where(x => _dbContext.LecturerAssignments.Any(a => a.LecturerProfileId == lecturerProfileId && a.CourseOfferingId == x.CourseOfferingId))
            .AsQueryable();

        if (query.CourseOfferingId.HasValue)
        {
            sessions = sessions.Where(x => x.CourseOfferingId == query.CourseOfferingId.Value);
        }

        return await sessions
            .OrderByDescending(x => x.SessionDate)
            .ThenByDescending(x => x.SessionNo)
            .Select(x => new AttendanceSessionListItemModel
            {
                Id = x.Id,
                CourseOfferingId = x.CourseOfferingId,
                CourseOfferingCode = x.CourseOffering != null ? x.CourseOffering.Code : string.Empty,
                CourseName = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Name : string.Empty,
                SemesterName = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.Name : string.Empty,
                SessionDate = x.SessionDate,
                SessionNo = x.SessionNo,
                Title = x.Title,
                RecordCount = x.Records.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AttendanceSessionModel> GetAttendanceSessionByIdAsync(Guid attendanceSessionId, CancellationToken cancellationToken = default)
    {
        var session = await _attendanceService.GetByIdAsync(new GetAttendanceSessionByIdQuery { Id = attendanceSessionId }, cancellationToken);
        await EnsureAssignedToCourseOfferingAsync(session.CourseOfferingId, cancellationToken);
        return session;
    }

    public async Task<AttendanceSessionModel> CreateAttendanceSessionAsync(CreateAttendanceSessionCommand command, CancellationToken cancellationToken = default)
    {
        await EnsureAssignedToCourseOfferingAsync(command.CourseOfferingId, cancellationToken);
        return await _attendanceService.CreateSessionAsync(command, cancellationToken);
    }

    public async Task<AttendanceSessionModel> UpdateAttendanceSessionAsync(UpdateAttendanceSessionCommand command, CancellationToken cancellationToken = default)
    {
        var session = await _attendanceService.GetByIdAsync(new GetAttendanceSessionByIdQuery { Id = command.Id }, cancellationToken);
        await EnsureAssignedToCourseOfferingAsync(session.CourseOfferingId, cancellationToken);
        return await _attendanceService.UpdateSessionAsync(command, cancellationToken);
    }

    public async Task<AttendanceSessionModel> UpdateAttendanceRecordsAsync(UpdateAttendanceRecordsCommand command, CancellationToken cancellationToken = default)
    {
        var session = await _attendanceService.GetByIdAsync(new GetAttendanceSessionByIdQuery { Id = command.Id }, cancellationToken);
        await EnsureAssignedToCourseOfferingAsync(session.CourseOfferingId, cancellationToken);
        return await _attendanceService.UpdateRecordsAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyCollection<GradeCategoryListItemModel>> GetGradeCategoriesAsync(GetLecturerGradeCategoriesQuery query, CancellationToken cancellationToken = default)
    {
        var lecturerProfileId = await _currentLecturerContext.GetRequiredLecturerProfileIdAsync(cancellationToken);
        var categories = _dbContext.GradeCategories
            .AsNoTracking()
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.Entries)
            .Where(x => _dbContext.LecturerAssignments.Any(a => a.LecturerProfileId == lecturerProfileId && a.CourseOfferingId == x.CourseOfferingId))
            .AsQueryable();

        if (query.CourseOfferingId.HasValue)
        {
            categories = categories.Where(x => x.CourseOfferingId == query.CourseOfferingId.Value);
        }

        return await categories
            .OrderBy(x => x.OrderIndex)
            .ThenBy(x => x.Name)
            .Select(x => new GradeCategoryListItemModel
            {
                Id = x.Id,
                CourseOfferingId = x.CourseOfferingId,
                CourseOfferingCode = x.CourseOffering != null ? x.CourseOffering.Code : string.Empty,
                CourseName = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Name : string.Empty,
                SemesterName = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.Name : string.Empty,
                Name = x.Name,
                Weight = x.Weight,
                MaxScore = x.MaxScore,
                OrderIndex = x.OrderIndex,
                IsActive = x.IsActive,
                EntryCount = x.Entries.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<GradeCategoryModel> GetGradeCategoryByIdAsync(Guid gradeCategoryId, CancellationToken cancellationToken = default)
    {
        var category = await _gradeService.GetByIdAsync(new GetGradeCategoryByIdQuery { Id = gradeCategoryId }, cancellationToken);
        await EnsureAssignedToCourseOfferingAsync(category.CourseOfferingId, cancellationToken);
        return category;
    }

    public async Task<GradeCategoryModel> CreateGradeCategoryAsync(CreateGradeCategoryCommand command, CancellationToken cancellationToken = default)
    {
        await EnsureAssignedToCourseOfferingAsync(command.CourseOfferingId, cancellationToken);
        return await _gradeService.CreateCategoryAsync(command, cancellationToken);
    }

    public async Task<GradeCategoryModel> UpdateGradeCategoryAsync(UpdateGradeCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var category = await _gradeService.GetByIdAsync(new GetGradeCategoryByIdQuery { Id = command.Id }, cancellationToken);
        await EnsureAssignedToCourseOfferingAsync(category.CourseOfferingId, cancellationToken);
        return await _gradeService.UpdateCategoryAsync(command, cancellationToken);
    }

    public async Task<GradeCategoryModel> UpdateGradeEntriesAsync(UpdateGradeEntriesCommand command, CancellationToken cancellationToken = default)
    {
        var category = await _gradeService.GetByIdAsync(new GetGradeCategoryByIdQuery { Id = command.Id }, cancellationToken);
        await EnsureAssignedToCourseOfferingAsync(category.CourseOfferingId, cancellationToken);
        await EnsureGradebookEditableAsync(category.CourseOfferingId, cancellationToken);
        return await _gradeService.UpdateEntriesAsync(command, cancellationToken);
    }

    public async Task<bool> IsGradebookEditableAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
    {
        await EnsureAssignedToCourseOfferingAsync(courseOfferingId, cancellationToken);
        return !await _dbContext.GradeResults
            .AnyAsync(x => x.CourseOfferingId == courseOfferingId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CourseMaterialListItemModel>> GetCourseMaterialsAsync(GetLecturerCourseMaterialsQuery query, CancellationToken cancellationToken = default)
    {
        var lecturerProfileId = await _currentLecturerContext.GetRequiredLecturerProfileIdAsync(cancellationToken);
        var materials = _dbContext.CourseMaterials
            .AsNoTracking()
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.FileMetadata)
            .Where(x => _dbContext.LecturerAssignments.Any(a => a.LecturerProfileId == lecturerProfileId && a.CourseOfferingId == x.CourseOfferingId))
            .AsQueryable();

        if (query.CourseOfferingId.HasValue)
        {
            materials = materials.Where(x => x.CourseOfferingId == query.CourseOfferingId.Value);
        }

        return await materials
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .Select(x => new CourseMaterialListItemModel
            {
                Id = x.Id,
                CourseOfferingId = x.CourseOfferingId,
                CourseOfferingCode = x.CourseOffering != null ? x.CourseOffering.Code : string.Empty,
                CourseName = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Name : string.Empty,
                SemesterName = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.Name : string.Empty,
                Title = x.Title,
                MaterialType = x.MaterialType,
                SortOrder = x.SortOrder,
                IsPublished = x.IsPublished,
                OriginalFileName = x.FileMetadata != null ? x.FileMetadata.OriginalFileName : string.Empty,
                ContentType = x.FileMetadata != null ? x.FileMetadata.ContentType : string.Empty,
                SizeInBytes = x.FileMetadata != null ? x.FileMetadata.SizeInBytes : 0,
                UploadedAtUtc = x.FileMetadata != null ? x.FileMetadata.UploadedAtUtc : DateTime.MinValue
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CourseMaterialModel> GetCourseMaterialByIdAsync(Guid courseMaterialId, CancellationToken cancellationToken = default)
    {
        var material = await _courseMaterialService.GetByIdAsync(new GetCourseMaterialByIdQuery { Id = courseMaterialId }, cancellationToken);
        await EnsureAssignedToCourseOfferingAsync(material.CourseOfferingId, cancellationToken);
        return material;
    }

    public async Task<CourseMaterialModel> UploadCourseMaterialAsync(UploadCourseMaterialCommand command, CancellationToken cancellationToken = default)
    {
        await EnsureAssignedToCourseOfferingAsync(command.CourseOfferingId, cancellationToken);
        return await _courseMaterialService.UploadAsync(command, cancellationToken);
    }

    public async Task<CourseMaterialModel> UpdateCourseMaterialAsync(UpdateCourseMaterialCommand command, CancellationToken cancellationToken = default)
    {
        var material = await _courseMaterialService.GetByIdAsync(new GetCourseMaterialByIdQuery { Id = command.Id }, cancellationToken);
        await EnsureAssignedToCourseOfferingAsync(material.CourseOfferingId, cancellationToken);
        return await _courseMaterialService.UpdateAsync(command, cancellationToken);
    }

    public async Task<CourseMaterialModel> SetCourseMaterialPublishStateAsync(SetCourseMaterialPublishStateCommand command, CancellationToken cancellationToken = default)
    {
        var material = await _courseMaterialService.GetByIdAsync(new GetCourseMaterialByIdQuery { Id = command.Id }, cancellationToken);
        await EnsureAssignedToCourseOfferingAsync(material.CourseOfferingId, cancellationToken);
        return await _courseMaterialService.SetPublishStateAsync(command, cancellationToken);
    }

    public async Task<FileDownloadModel> DownloadCourseMaterialAsync(Guid courseMaterialId, CancellationToken cancellationToken = default)
    {
        var material = await _courseMaterialService.GetByIdAsync(new GetCourseMaterialByIdQuery { Id = courseMaterialId }, cancellationToken);
        await EnsureAssignedToCourseOfferingAsync(material.CourseOfferingId, cancellationToken);
        return await _courseMaterialService.DownloadAsync(new DownloadCourseMaterialQuery { Id = courseMaterialId }, cancellationToken);
    }

    public async Task<IReadOnlyCollection<GradeResultListItemModel>> GetGradeResultsAsync(GetLecturerGradeResultsQuery query, CancellationToken cancellationToken = default)
    {
        var lecturerProfileId = await _currentLecturerContext.GetRequiredLecturerProfileIdAsync(cancellationToken);
        var results = _dbContext.GradeResults
            .AsNoTracking()
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.RosterItem)
            .Where(x => _dbContext.LecturerAssignments.Any(a => a.LecturerProfileId == lecturerProfileId && a.CourseOfferingId == x.CourseOfferingId))
            .AsQueryable();

        if (query.CourseOfferingId.HasValue)
        {
            results = results.Where(x => x.CourseOfferingId == query.CourseOfferingId.Value);
        }

        return await results
            .OrderBy(x => x.CourseOffering!.Code)
            .ThenBy(x => x.RosterItem!.StudentCode)
            .Select(x => new GradeResultListItemModel
            {
                Id = x.Id,
                CourseOfferingId = x.CourseOfferingId,
                CourseOfferingCode = x.CourseOffering != null ? x.CourseOffering.Code : string.Empty,
                CourseName = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Name : string.Empty,
                SemesterName = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.Name : string.Empty,
                RosterItemId = x.RosterItemId,
                StudentCode = x.RosterItem != null ? x.RosterItem.StudentCode : string.Empty,
                StudentFullName = x.RosterItem != null ? x.RosterItem.StudentFullName : string.Empty,
                WeightedFinalScore = x.WeightedFinalScore,
                PassingScore = x.PassingScore,
                IsPassed = x.IsPassed,
                CalculatedAtUtc = x.CalculatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    private async Task EnsureAssignedToCourseOfferingAsync(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        var lecturerProfileId = await _currentLecturerContext.GetRequiredLecturerProfileIdAsync(cancellationToken);
        var isAssigned = await _dbContext.LecturerAssignments
            .AnyAsync(x => x.CourseOfferingId == courseOfferingId && x.LecturerProfileId == lecturerProfileId, cancellationToken);

        if (!isAssigned)
        {
            throw new AuthException("Current lecturer is not assigned to the course offering.");
        }
    }

    private async Task EnsureGradebookEditableAsync(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        var hasGradeResults = await _dbContext.GradeResults
            .AnyAsync(x => x.CourseOfferingId == courseOfferingId, cancellationToken);

        if (hasGradeResults)
        {
            throw new AuthException("This class grade book was already finalized. Student scores can no longer be changed from the lecturer portal.");
        }
    }
}
