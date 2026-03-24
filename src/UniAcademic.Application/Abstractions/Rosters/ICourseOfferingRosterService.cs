using UniAcademic.Application.Models.Rosters;

namespace UniAcademic.Application.Abstractions.Rosters;

public interface ICourseOfferingRosterService
{
    Task<CourseOfferingRosterModel> GetByCourseOfferingIdAsync(GetCourseOfferingRosterQuery query, CancellationToken cancellationToken = default);

    Task<CourseOfferingRosterModel> FinalizeAsync(FinalizeCourseOfferingRosterCommand command, CancellationToken cancellationToken = default);
}
