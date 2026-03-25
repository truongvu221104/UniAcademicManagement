using UniAcademic.Contracts.GradeResults;

namespace UniAcademic.AdminApp.Services.GradeResults;

public interface IGradeResultApiClient
{
    Task<IReadOnlyCollection<GradeResultListItemResponse>> GetListAsync(Guid? courseOfferingId = null, CancellationToken cancellationToken = default);

    Task<GradeResultResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GradeResultResponse>> CalculateAsync(CalculateGradeResultsRequest request, CancellationToken cancellationToken = default);
}
