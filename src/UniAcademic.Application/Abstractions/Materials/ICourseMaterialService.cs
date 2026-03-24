using UniAcademic.Application.Models.Materials;

namespace UniAcademic.Application.Abstractions.Materials;

public interface ICourseMaterialService
{
    Task<CourseMaterialModel> UploadAsync(UploadCourseMaterialCommand command, CancellationToken cancellationToken = default);

    Task<CourseMaterialModel> UpdateAsync(UpdateCourseMaterialCommand command, CancellationToken cancellationToken = default);

    Task<CourseMaterialModel> SetPublishStateAsync(SetCourseMaterialPublishStateCommand command, CancellationToken cancellationToken = default);

    Task<CourseMaterialModel> GetByIdAsync(GetCourseMaterialByIdQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CourseMaterialListItemModel>> GetListAsync(GetCourseMaterialsQuery query, CancellationToken cancellationToken = default);

    Task<FileDownloadModel> DownloadAsync(DownloadCourseMaterialQuery query, CancellationToken cancellationToken = default);
}
