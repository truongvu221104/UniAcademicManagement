using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Materials;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Abstractions.StudentPortal;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.CourseOfferings;
using UniAcademic.Application.Models.GradeResults;
using UniAcademic.Application.Models.Materials;
using UniAcademic.Application.Models.StudentPortal;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Features.StudentPortal;

public sealed class StudentPortalService : IStudentPortalService
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentStudentContext _currentStudentContext;
    private readonly ICourseMaterialService _courseMaterialService;

    public StudentPortalService(
        IAppDbContext dbContext,
        ICurrentStudentContext currentStudentContext,
        ICourseMaterialService courseMaterialService)
    {
        _dbContext = dbContext;
        _currentStudentContext = currentStudentContext;
        _courseMaterialService = courseMaterialService;
    }

    public async Task<IReadOnlyCollection<StudentSelfEnrollCourseOfferingItemModel>> GetSelfEnrollCourseOfferingsAsync(GetSelfEnrollCourseOfferingsQuery query, CancellationToken cancellationToken = default)
    {
        var studentProfileId = await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);

        var offerings = _dbContext.CourseOfferings
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Semester)
            .Where(x => x.Status == CourseOfferingStatus.Active
                && x.Semester != null
                && x.Semester.Status == SemesterStatus.Active)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            offerings = offerings.Where(x =>
                x.Code.Contains(keyword)
                || x.DisplayName.Contains(keyword)
                || (x.Course != null && (x.Course.Code.Contains(keyword) || x.Course.Name.Contains(keyword))));
        }

        var enrolledOfferingIds = await _dbContext.Enrollments
            .Where(x => x.StudentProfileId == studentProfileId && x.Status == EnrollmentStatus.Enrolled)
            .Select(x => x.CourseOfferingId)
            .ToListAsync(cancellationToken);

        var enrolledCounts = await _dbContext.Enrollments
            .Where(x => x.Status == EnrollmentStatus.Enrolled)
            .GroupBy(x => x.CourseOfferingId)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var items = await offerings
            .OrderBy(x => x.Code)
            .Select(x => new StudentSelfEnrollCourseOfferingItemModel
            {
                Id = x.Id,
                CourseId = x.CourseId,
                Code = x.Code,
                DisplayName = x.DisplayName,
                CourseCode = x.Course != null ? x.Course.Code : string.Empty,
                CourseName = x.Course != null ? x.Course.Name : string.Empty,
                SemesterName = x.Semester != null ? x.Semester.Name : string.Empty,
                Credits = x.Course != null ? x.Course.Credits : 0,
                Capacity = x.Capacity,
                DayOfWeek = x.DayOfWeek,
                StartPeriod = x.StartPeriod,
                EndPeriod = x.EndPeriod,
                IsRosterFinalized = x.IsRosterFinalized
            })
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.EnrolledCount = enrolledCounts.GetValueOrDefault(item.Id);
            item.IsAlreadyEnrolled = enrolledOfferingIds.Contains(item.Id);
        }

        return items;
    }

    public async Task<StudentSelfEnrollCourseOfferingDetailModel> GetSelfEnrollCourseOfferingByIdAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
    {
        var studentProfileId = await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);

        var offering = await _dbContext.CourseOfferings
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Semester)
            .Where(x => x.Id == courseOfferingId
                && x.Status == CourseOfferingStatus.Active
                && x.Semester != null
                && x.Semester.Status == SemesterStatus.Active)
            .Select(x => new StudentSelfEnrollCourseOfferingDetailModel
            {
                Id = x.Id,
                CourseId = x.CourseId,
                Code = x.Code,
                DisplayName = x.DisplayName,
                CourseCode = x.Course != null ? x.Course.Code : string.Empty,
                CourseName = x.Course != null ? x.Course.Name : string.Empty,
                SemesterCode = x.Semester != null ? x.Semester.Code : string.Empty,
                SemesterName = x.Semester != null ? x.Semester.Name : string.Empty,
                Credits = x.Course != null ? x.Course.Credits : 0,
                Capacity = x.Capacity,
                DayOfWeek = x.DayOfWeek,
                StartPeriod = x.StartPeriod,
                EndPeriod = x.EndPeriod,
                IsRosterFinalized = x.IsRosterFinalized,
                Description = x.Description
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AuthException("Course offering was not found.");

        offering.EnrolledCount = await _dbContext.Enrollments
            .CountAsync(x => x.CourseOfferingId == courseOfferingId && x.Status == EnrollmentStatus.Enrolled, cancellationToken);

        offering.IsAlreadyEnrolled = await _dbContext.Enrollments
            .AnyAsync(x => x.StudentProfileId == studentProfileId
                && x.CourseOfferingId == courseOfferingId
                && x.Status == EnrollmentStatus.Enrolled, cancellationToken);

        return offering;
    }

    public async Task<IReadOnlyCollection<StudentCurrentEnrollmentItemModel>> GetMyCurrentEnrollmentsAsync(CancellationToken cancellationToken = default)
    {
        var studentProfileId = await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);

        return await _dbContext.Enrollments
            .AsNoTracking()
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Where(x => x.StudentProfileId == studentProfileId && x.Status == EnrollmentStatus.Enrolled)
            .OrderBy(x => x.CourseOffering!.Code)
            .Select(x => new StudentCurrentEnrollmentItemModel
            {
                EnrollmentId = x.Id,
                CourseOfferingId = x.CourseOfferingId,
                CourseOfferingCode = x.CourseOffering != null ? x.CourseOffering.Code : string.Empty,
                CourseCode = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Code : string.Empty,
                CourseName = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Name : string.Empty,
                SemesterName = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.Name : string.Empty,
                Credits = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Credits : 0,
                DayOfWeek = x.CourseOffering != null ? x.CourseOffering.DayOfWeek : 0,
                StartPeriod = x.CourseOffering != null ? x.CourseOffering.StartPeriod : 0,
                EndPeriod = x.CourseOffering != null ? x.CourseOffering.EndPeriod : 0,
                IsRosterFinalized = x.CourseOffering != null && x.CourseOffering.IsRosterFinalized,
                EnrolledAtUtc = x.EnrolledAtUtc,
                Note = x.Note
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<StudentCurrentEnrollmentItemModel> GetMyCurrentEnrollmentByIdAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
    {
        var studentProfileId = await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);

        return await _dbContext.Enrollments
            .AsNoTracking()
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Where(x => x.Id == enrollmentId
                && x.StudentProfileId == studentProfileId
                && x.Status == EnrollmentStatus.Enrolled)
            .Select(x => new StudentCurrentEnrollmentItemModel
            {
                EnrollmentId = x.Id,
                CourseOfferingId = x.CourseOfferingId,
                CourseOfferingCode = x.CourseOffering != null ? x.CourseOffering.Code : string.Empty,
                CourseCode = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Code : string.Empty,
                CourseName = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Name : string.Empty,
                SemesterName = x.CourseOffering != null && x.CourseOffering.Semester != null ? x.CourseOffering.Semester.Name : string.Empty,
                Credits = x.CourseOffering != null && x.CourseOffering.Course != null ? x.CourseOffering.Course.Credits : 0,
                DayOfWeek = x.CourseOffering != null ? x.CourseOffering.DayOfWeek : 0,
                StartPeriod = x.CourseOffering != null ? x.CourseOffering.StartPeriod : 0,
                EndPeriod = x.CourseOffering != null ? x.CourseOffering.EndPeriod : 0,
                IsRosterFinalized = x.CourseOffering != null && x.CourseOffering.IsRosterFinalized,
                EnrolledAtUtc = x.EnrolledAtUtc,
                Note = x.Note
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AuthException("Enrollment was not found.");
    }

    public async Task<IReadOnlyCollection<CourseOfferingListItemModel>> GetMyCourseOfferingsAsync(GetMyCourseOfferingsQuery query, CancellationToken cancellationToken = default)
    {
        var studentProfileId = await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);
        var enrollments = _dbContext.Enrollments
            .AsNoTracking()
            .Where(x => x.StudentProfileId == studentProfileId && x.Status == EnrollmentStatus.Enrolled)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            enrollments = enrollments.Where(x =>
                x.CourseOffering != null &&
                (x.CourseOffering.Code.Contains(keyword)
                    || x.CourseOffering.DisplayName.Contains(keyword)
                    || (x.CourseOffering.Course != null && x.CourseOffering.Course.Name.Contains(keyword))));
        }

        return await enrollments
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

    public async Task<IReadOnlyCollection<StudentAttendanceItemModel>> GetMyAttendanceAsync(GetMyAttendanceQuery query, CancellationToken cancellationToken = default)
    {
        var studentProfileId = await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);
        var records = _dbContext.AttendanceRecords
            .AsNoTracking()
            .Include(x => x.AttendanceSession)
                .ThenInclude(x => x!.CourseOffering)
                    .ThenInclude(x => x!.Course)
            .Include(x => x.AttendanceSession)
                .ThenInclude(x => x!.CourseOffering)
                    .ThenInclude(x => x!.Semester)
            .Include(x => x.RosterItem)
            .Where(x => x.RosterItem != null && x.RosterItem.StudentProfileId == studentProfileId)
            .AsQueryable();

        if (query.CourseOfferingId.HasValue)
        {
            records = records.Where(x => x.AttendanceSession != null && x.AttendanceSession.CourseOfferingId == query.CourseOfferingId.Value);
        }

        return await records
            .OrderByDescending(x => x.AttendanceSession!.SessionDate)
            .ThenByDescending(x => x.AttendanceSession!.SessionNo)
            .Select(x => new StudentAttendanceItemModel
            {
                AttendanceSessionId = x.AttendanceSessionId,
                CourseOfferingId = x.AttendanceSession != null ? x.AttendanceSession.CourseOfferingId : Guid.Empty,
                CourseOfferingCode = x.AttendanceSession != null && x.AttendanceSession.CourseOffering != null ? x.AttendanceSession.CourseOffering.Code : string.Empty,
                CourseName = x.AttendanceSession != null && x.AttendanceSession.CourseOffering != null && x.AttendanceSession.CourseOffering.Course != null ? x.AttendanceSession.CourseOffering.Course.Name : string.Empty,
                SemesterName = x.AttendanceSession != null && x.AttendanceSession.CourseOffering != null && x.AttendanceSession.CourseOffering.Semester != null ? x.AttendanceSession.CourseOffering.Semester.Name : string.Empty,
                SessionDate = x.AttendanceSession != null ? x.AttendanceSession.SessionDate : DateTime.MinValue,
                SessionNo = x.AttendanceSession != null ? x.AttendanceSession.SessionNo : 0,
                Title = x.AttendanceSession != null ? x.AttendanceSession.Title : null,
                Status = x.Status,
                Note = x.Note
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<StudentGradeItemModel>> GetMyGradesAsync(GetMyGradesQuery query, CancellationToken cancellationToken = default)
    {
        var studentProfileId = await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);
        var entries = _dbContext.GradeEntries
            .AsNoTracking()
            .Include(x => x.GradeCategory)
                .ThenInclude(x => x!.CourseOffering)
                    .ThenInclude(x => x!.Course)
            .Include(x => x.GradeCategory)
                .ThenInclude(x => x!.CourseOffering)
                    .ThenInclude(x => x!.Semester)
            .Include(x => x.RosterItem)
            .Where(x => x.RosterItem != null
                && x.RosterItem.StudentProfileId == studentProfileId
                && x.GradeCategory != null
                && x.GradeCategory.IsActive)
            .AsQueryable();

        if (query.CourseOfferingId.HasValue)
        {
            entries = entries.Where(x => x.GradeCategory != null && x.GradeCategory.CourseOfferingId == query.CourseOfferingId.Value);
        }

        return await entries
            .OrderBy(x => x.GradeCategory!.OrderIndex)
            .ThenBy(x => x.GradeCategory!.Name)
            .Select(x => new StudentGradeItemModel
            {
                GradeCategoryId = x.GradeCategoryId,
                CourseOfferingId = x.GradeCategory != null ? x.GradeCategory.CourseOfferingId : Guid.Empty,
                CourseOfferingCode = x.GradeCategory != null && x.GradeCategory.CourseOffering != null ? x.GradeCategory.CourseOffering.Code : string.Empty,
                CourseName = x.GradeCategory != null && x.GradeCategory.CourseOffering != null && x.GradeCategory.CourseOffering.Course != null ? x.GradeCategory.CourseOffering.Course.Name : string.Empty,
                SemesterName = x.GradeCategory != null && x.GradeCategory.CourseOffering != null && x.GradeCategory.CourseOffering.Semester != null ? x.GradeCategory.CourseOffering.Semester.Name : string.Empty,
                CategoryName = x.GradeCategory != null ? x.GradeCategory.Name : string.Empty,
                Weight = x.GradeCategory != null ? x.GradeCategory.Weight : 0,
                MaxScore = x.GradeCategory != null ? x.GradeCategory.MaxScore : 0,
                Score = x.Score,
                Note = x.Note
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<GradeResultListItemModel>> GetMyGradeResultsAsync(GetMyGradeResultsQuery query, CancellationToken cancellationToken = default)
    {
        var studentProfileId = await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);
        var results = _dbContext.GradeResults
            .AsNoTracking()
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.RosterItem)
            .Where(x => x.RosterItem != null && x.RosterItem.StudentProfileId == studentProfileId)
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

    public async Task<IReadOnlyCollection<CourseMaterialListItemModel>> GetMyMaterialsAsync(GetMyMaterialsQuery query, CancellationToken cancellationToken = default)
    {
        var studentProfileId = await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);
        var enrolledOfferingIds = _dbContext.Enrollments
            .Where(x => x.StudentProfileId == studentProfileId && x.Status == EnrollmentStatus.Enrolled)
            .Select(x => x.CourseOfferingId);

        var materials = _dbContext.CourseMaterials
            .AsNoTracking()
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.FileMetadata)
            .Where(x => x.IsPublished && enrolledOfferingIds.Contains(x.CourseOfferingId))
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

    public async Task<FileDownloadModel> DownloadMaterialAsync(Guid courseMaterialId, CancellationToken cancellationToken = default)
    {
        var studentProfileId = await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);
        var isAccessible = await _dbContext.CourseMaterials
            .AnyAsync(x =>
                x.Id == courseMaterialId
                && x.IsPublished
                && _dbContext.Enrollments.Any(e => e.StudentProfileId == studentProfileId && e.Status == EnrollmentStatus.Enrolled && e.CourseOfferingId == x.CourseOfferingId),
                cancellationToken);

        if (!isAccessible)
        {
            throw new AuthException("Course material was not found.");
        }

        return await _courseMaterialService.DownloadAsync(new DownloadCourseMaterialQuery { Id = courseMaterialId }, cancellationToken);
    }
}
