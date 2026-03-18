using UniAcademic.Contracts.Faculties;

namespace UniAcademic.AdminApp.Services.Faculties;

public interface IFacultyApiClient
{
    Task<IReadOnlyCollection<FacultyListItemResponse>> GetListAsync(string? keyword = null, string? status = null, CancellationToken cancellationToken = default);

    Task<FacultyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FacultyResponse> CreateAsync(CreateFacultyRequest request, CancellationToken cancellationToken = default);

    Task<FacultyResponse> UpdateAsync(Guid id, UpdateFacultyRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
