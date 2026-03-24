using UniAcademic.Application.Models.Grades;

namespace UniAcademic.Application.Abstractions.Grades;

public interface IGradeService
{
    Task<GradeCategoryModel> CreateCategoryAsync(CreateGradeCategoryCommand command, CancellationToken cancellationToken = default);

    Task<GradeCategoryModel> UpdateCategoryAsync(UpdateGradeCategoryCommand command, CancellationToken cancellationToken = default);

    Task<GradeCategoryModel> UpdateEntriesAsync(UpdateGradeEntriesCommand command, CancellationToken cancellationToken = default);

    Task<GradeCategoryModel> GetByIdAsync(GetGradeCategoryByIdQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GradeCategoryListItemModel>> GetListAsync(GetGradeCategoriesQuery query, CancellationToken cancellationToken = default);
}
