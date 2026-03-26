using UniAcademic.Application.Models.CourseOfferings;
using UniAcademic.Application.Models.GradeResults;
using UniAcademic.Application.Models.Materials;
using UniAcademic.Application.Models.StudentPortal;

namespace UniAcademic.Application.Abstractions.StudentPortal;

public interface IStudentPortalService
{
    Task<IReadOnlyCollection<StudentSelfEnrollCourseOfferingItemModel>> GetSelfEnrollCourseOfferingsAsync(GetSelfEnrollCourseOfferingsQuery query, CancellationToken cancellationToken = default);

    Task<StudentSelfEnrollCourseOfferingDetailModel> GetSelfEnrollCourseOfferingByIdAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StudentCurrentEnrollmentItemModel>> GetMyCurrentEnrollmentsAsync(CancellationToken cancellationToken = default);

    Task<StudentCurrentEnrollmentItemModel> GetMyCurrentEnrollmentByIdAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CourseOfferingListItemModel>> GetMyCourseOfferingsAsync(GetMyCourseOfferingsQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StudentAttendanceItemModel>> GetMyAttendanceAsync(GetMyAttendanceQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StudentGradeItemModel>> GetMyGradesAsync(GetMyGradesQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GradeResultListItemModel>> GetMyGradeResultsAsync(GetMyGradeResultsQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CourseMaterialListItemModel>> GetMyMaterialsAsync(GetMyMaterialsQuery query, CancellationToken cancellationToken = default);

    Task<FileDownloadModel> DownloadMaterialAsync(Guid courseMaterialId, CancellationToken cancellationToken = default);
}
