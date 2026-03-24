using UniAcademic.Contracts.Rosters;

namespace UniAcademic.AdminApp.Services.Rosters;

public interface ICourseOfferingRosterApiClient
{
    Task<CourseOfferingRosterResponse> GetAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);

    Task<CourseOfferingRosterResponse> FinalizeAsync(Guid courseOfferingId, FinalizeCourseOfferingRosterRequest request, CancellationToken cancellationToken = default);
}
