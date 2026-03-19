using UniAcademic.Contracts.Courses;

namespace UniAcademic.AdminApp.Services.Courses;

public interface ICourseApiClient
{
    Task<IReadOnlyCollection<CourseListItemResponse>> GetListAsync(string? keyword = null, Guid? facultyId = null, string? status = null, CancellationToken cancellationToken = default);

    Task<CourseResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CourseResponse> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken = default);

    Task<CourseResponse> UpdateAsync(Guid id, UpdateCourseRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
