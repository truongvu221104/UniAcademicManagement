using UniAcademic.Contracts.Enrollments;

namespace UniAcademic.AdminApp.Services.Enrollments;

public interface IEnrollmentApiClient
{
    Task<IReadOnlyCollection<EnrollmentListItemResponse>> GetListAsync(
        string? keyword = null,
        Guid? studentProfileId = null,
        Guid? courseOfferingId = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<EnrollmentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<EnrollmentResponse> CreateAsync(CreateEnrollmentRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
