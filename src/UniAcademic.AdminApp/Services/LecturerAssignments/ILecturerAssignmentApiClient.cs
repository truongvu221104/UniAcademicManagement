using UniAcademic.Contracts.LecturerAssignments;

namespace UniAcademic.AdminApp.Services.LecturerAssignments;

public interface ILecturerAssignmentApiClient
{
    Task<IReadOnlyCollection<LecturerAssignmentResponse>> GetListAsync(Guid? courseOfferingId = null, Guid? lecturerProfileId = null, CancellationToken cancellationToken = default);

    Task<LecturerAssignmentResponse> AssignAsync(AssignLecturerRequest request, CancellationToken cancellationToken = default);

    Task UnassignAsync(Guid id, CancellationToken cancellationToken = default);
}
