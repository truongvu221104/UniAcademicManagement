using UniAcademic.Contracts.Grades;

namespace UniAcademic.AdminApp.Services.Grades;

public interface IGradeApiClient
{
    Task<IReadOnlyCollection<GradeCategoryListItemResponse>> GetListAsync(Guid? courseOfferingId = null, CancellationToken cancellationToken = default);

    Task<GradeCategoryResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GradeCategoryResponse> CreateCategoryAsync(CreateGradeCategoryRequest request, CancellationToken cancellationToken = default);

    Task<GradeCategoryResponse> UpdateCategoryAsync(Guid id, UpdateGradeCategoryRequest request, CancellationToken cancellationToken = default);

    Task<GradeCategoryResponse> UpdateEntriesAsync(Guid id, UpdateGradeEntriesRequest request, CancellationToken cancellationToken = default);
}
