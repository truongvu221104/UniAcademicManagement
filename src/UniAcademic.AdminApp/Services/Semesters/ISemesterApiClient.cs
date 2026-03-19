using UniAcademic.Contracts.Semesters;

namespace UniAcademic.AdminApp.Services.Semesters;

public interface ISemesterApiClient
{
    Task<IReadOnlyCollection<SemesterListItemResponse>> GetListAsync(string? keyword = null, string? academicYear = null, int? termNo = null, string? status = null, CancellationToken cancellationToken = default);

    Task<SemesterResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SemesterResponse> CreateAsync(CreateSemesterRequest request, CancellationToken cancellationToken = default);

    Task<SemesterResponse> UpdateAsync(Guid id, UpdateSemesterRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
