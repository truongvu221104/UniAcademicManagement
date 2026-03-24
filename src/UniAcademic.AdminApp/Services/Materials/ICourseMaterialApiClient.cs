using UniAcademic.Contracts.Materials;

namespace UniAcademic.AdminApp.Services.Materials;

public interface ICourseMaterialApiClient
{
    Task<IReadOnlyCollection<CourseMaterialListItemResponse>> GetListAsync(Guid? courseOfferingId = null, CancellationToken cancellationToken = default);

    Task<CourseMaterialResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CourseMaterialResponse> UploadAsync(UploadCourseMaterialRequest request, string filePath, CancellationToken cancellationToken = default);

    Task<CourseMaterialResponse> UpdateAsync(Guid id, UpdateCourseMaterialRequest request, CancellationToken cancellationToken = default);

    Task<CourseMaterialResponse> SetPublishStateAsync(Guid id, SetCourseMaterialPublishStateRequest request, CancellationToken cancellationToken = default);

    Task<byte[]> DownloadAsync(Guid id, CancellationToken cancellationToken = default);
}
