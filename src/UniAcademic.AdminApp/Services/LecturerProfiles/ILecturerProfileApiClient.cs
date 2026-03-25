using UniAcademic.Contracts.LecturerProfiles;

namespace UniAcademic.AdminApp.Services.LecturerProfiles;

public interface ILecturerProfileApiClient
{
    Task<IReadOnlyCollection<LecturerProfileListItemResponse>> GetListAsync(Guid? facultyId = null, bool? isActive = null, string? keyword = null, CancellationToken cancellationToken = default);

    Task<LecturerProfileResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LecturerProfileResponse> CreateAsync(CreateLecturerProfileRequest request, CancellationToken cancellationToken = default);

    Task<LecturerProfileResponse> UpdateAsync(Guid id, UpdateLecturerProfileRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
