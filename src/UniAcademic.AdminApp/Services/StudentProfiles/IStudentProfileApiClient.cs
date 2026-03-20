using UniAcademic.Contracts.StudentProfiles;

namespace UniAcademic.AdminApp.Services.StudentProfiles;

public interface IStudentProfileApiClient
{
    Task<IReadOnlyCollection<StudentProfileListItemResponse>> GetListAsync(
        string? keyword = null,
        Guid? studentClassId = null,
        string? gender = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<StudentProfileResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<StudentProfileResponse> CreateAsync(CreateStudentProfileRequest request, CancellationToken cancellationToken = default);

    Task<StudentProfileResponse> UpdateAsync(Guid id, UpdateStudentProfileRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
