using UniAcademic.Application.Models.Attendance;
using UniAcademic.Application.Models.CourseOfferings;
using UniAcademic.Application.Models.Grades;
using UniAcademic.Application.Models.GradeResults;
using UniAcademic.Application.Models.LecturerPortal;
using UniAcademic.Application.Models.Materials;

namespace UniAcademic.Application.Abstractions.LecturerPortal;

public interface ILecturerPortalService
{
    Task<IReadOnlyCollection<CourseOfferingListItemModel>> GetMyTeachingOfferingsAsync(GetMyTeachingOfferingsQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AttendanceSessionListItemModel>> GetAttendanceSessionsAsync(GetLecturerAttendanceSessionsQuery query, CancellationToken cancellationToken = default);

    Task<AttendanceSessionModel> GetAttendanceSessionByIdAsync(Guid attendanceSessionId, CancellationToken cancellationToken = default);

    Task<AttendanceSessionModel> CreateAttendanceSessionAsync(CreateAttendanceSessionCommand command, CancellationToken cancellationToken = default);

    Task<AttendanceSessionModel> UpdateAttendanceRecordsAsync(UpdateAttendanceRecordsCommand command, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GradeCategoryListItemModel>> GetGradeCategoriesAsync(GetLecturerGradeCategoriesQuery query, CancellationToken cancellationToken = default);

    Task<GradeCategoryModel> GetGradeCategoryByIdAsync(Guid gradeCategoryId, CancellationToken cancellationToken = default);

    Task<GradeCategoryModel> CreateGradeCategoryAsync(CreateGradeCategoryCommand command, CancellationToken cancellationToken = default);

    Task<GradeCategoryModel> UpdateGradeCategoryAsync(UpdateGradeCategoryCommand command, CancellationToken cancellationToken = default);

    Task<GradeCategoryModel> UpdateGradeEntriesAsync(UpdateGradeEntriesCommand command, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CourseMaterialListItemModel>> GetCourseMaterialsAsync(GetLecturerCourseMaterialsQuery query, CancellationToken cancellationToken = default);

    Task<CourseMaterialModel> GetCourseMaterialByIdAsync(Guid courseMaterialId, CancellationToken cancellationToken = default);

    Task<CourseMaterialModel> UploadCourseMaterialAsync(UploadCourseMaterialCommand command, CancellationToken cancellationToken = default);

    Task<CourseMaterialModel> UpdateCourseMaterialAsync(UpdateCourseMaterialCommand command, CancellationToken cancellationToken = default);

    Task<CourseMaterialModel> SetCourseMaterialPublishStateAsync(SetCourseMaterialPublishStateCommand command, CancellationToken cancellationToken = default);

    Task<FileDownloadModel> DownloadCourseMaterialAsync(Guid courseMaterialId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GradeResultListItemModel>> GetGradeResultsAsync(GetLecturerGradeResultsQuery query, CancellationToken cancellationToken = default);
}
