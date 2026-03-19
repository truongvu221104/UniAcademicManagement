using UniAcademic.Application.Models.Courses;

namespace UniAcademic.Application.Abstractions.Courses;

public interface ICourseService
{
    Task<CourseModel> CreateAsync(CreateCourseCommand command, CancellationToken cancellationToken = default);

    Task<CourseModel> UpdateAsync(UpdateCourseCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(DeleteCourseCommand command, CancellationToken cancellationToken = default);

    Task<CourseModel> GetByIdAsync(GetCourseByIdQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CourseListItemModel>> GetListAsync(GetCoursesQuery query, CancellationToken cancellationToken = default);
}
