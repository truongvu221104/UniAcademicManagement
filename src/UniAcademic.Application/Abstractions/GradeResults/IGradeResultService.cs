using UniAcademic.Application.Models.GradeResults;

namespace UniAcademic.Application.Abstractions.GradeResults;

public interface IGradeResultService
{
    Task<IReadOnlyCollection<GradeResultModel>> CalculateAsync(CalculateGradeResultsCommand command, CancellationToken cancellationToken = default);

    Task<GradeResultModel> GetByIdAsync(GetGradeResultByIdQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GradeResultListItemModel>> GetListAsync(GetGradeResultsQuery query, CancellationToken cancellationToken = default);
}
