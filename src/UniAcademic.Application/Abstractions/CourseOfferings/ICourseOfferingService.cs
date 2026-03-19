using UniAcademic.Application.Models.CourseOfferings;

namespace UniAcademic.Application.Abstractions.CourseOfferings;

public interface ICourseOfferingService
{
    Task<CourseOfferingModel> CreateAsync(CreateCourseOfferingCommand command, CancellationToken cancellationToken = default);

    Task<CourseOfferingModel> UpdateAsync(UpdateCourseOfferingCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(DeleteCourseOfferingCommand command, CancellationToken cancellationToken = default);

    Task<CourseOfferingModel> GetByIdAsync(GetCourseOfferingByIdQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CourseOfferingListItemModel>> GetListAsync(GetCourseOfferingsQuery query, CancellationToken cancellationToken = default);
}
