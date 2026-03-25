using UniAcademic.Application.Models.CourseOfferings;
using UniAcademic.Application.Models.GradeResults;
using UniAcademic.Application.Models.Materials;
using UniAcademic.Application.Models.StudentPortal;

namespace UniAcademic.Application.Abstractions.StudentPortal;

public interface IStudentPortalService
{
    Task<IReadOnlyCollection<CourseOfferingListItemModel>> GetMyCourseOfferingsAsync(GetMyCourseOfferingsQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StudentAttendanceItemModel>> GetMyAttendanceAsync(GetMyAttendanceQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StudentGradeItemModel>> GetMyGradesAsync(GetMyGradesQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GradeResultListItemModel>> GetMyGradeResultsAsync(GetMyGradeResultsQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CourseMaterialListItemModel>> GetMyMaterialsAsync(GetMyMaterialsQuery query, CancellationToken cancellationToken = default);

    Task<FileDownloadModel> DownloadMaterialAsync(Guid courseMaterialId, CancellationToken cancellationToken = default);
}
