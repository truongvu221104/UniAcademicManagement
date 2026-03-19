using UniAcademic.Contracts.CourseOfferings;

namespace UniAcademic.AdminApp.Services.CourseOfferings;

public interface ICourseOfferingApiClient
{
    Task<IReadOnlyCollection<CourseOfferingListItemResponse>> GetListAsync(
        string? keyword = null,
        Guid? courseId = null,
        Guid? semesterId = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<CourseOfferingResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CourseOfferingResponse> CreateAsync(CreateCourseOfferingRequest request, CancellationToken cancellationToken = default);

    Task<CourseOfferingResponse> UpdateAsync(Guid id, UpdateCourseOfferingRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
